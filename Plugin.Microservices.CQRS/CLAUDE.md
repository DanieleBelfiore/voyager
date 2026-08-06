# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this architecture variant. See the [repo-root README](../README.md) for how this fits into the wider architecture portfolio.

## Commands

```bash
# Start infrastructure (SQL Server, Redis, RabbitMQ)
docker-compose up -d

# Build solution
dotnet build Voyager.sln

# Run all tests
dotnet test Voyager.sln

# Run tests for a specific service
dotnet test Driver/Driver.Tests/Driver.Tests.csproj
dotnet test Ride/Ride.Tests/Ride.Tests.csproj

# Run a single test by name
dotnet test Driver/Driver.Tests/Driver.Tests.csproj --filter "FullyQualifiedName~AddDriverHandlerTests"

# Run the end-to-end demo (requires infrastructure running)
dotnet run --project Demo/Demo.csproj

# Format code
dotnet format Voyager.sln

# Teardown
docker-compose down -v --rmi local
```

Run all commands from inside `Plugin.Microservices.CQRS/`.

Services run on:
- Hub API: http://localhost:5000
- Identity API: http://localhost:5001
- Driver API: http://localhost:5002
- Ride API: http://localhost:5003

## Architecture

**Voyager** is a ride-sharing backend built as four independent microservices on .NET 10, communicating via RabbitMQ and exposing real-time events through SignalR.

### Services

| Service | Port | Responsibility |
|---------|------|----------------|
| Identity | 5001 | Auth, user registration, OAuth2/OpenID (OpenIddict) |
| Driver | 5002 | Driver registration, location updates, availability, matching |
| Ride | 5003 | Ride lifecycle: request → accept → start → complete → rate |
| Hub | 5000 | Real-time SignalR hub: broadcasts location/status events to riders |

### Layer Pattern (per service)

Each service follows a strict four-project split:

```
{Service}/              → ASP.NET Core web host (Program.cs, Dockerfile)
{Service}.API/          → Controllers (dispatch to Hikyaku)
{Service}.Core/         → CQRS commands/queries, DTOs, enums (no dependencies)
{Service}.Handlers/     → Handlers, EF DbContext, repositories, Mapperly profiles, EF migrations
```

Test projects (`{Service}.Tests`, `{Service}.IntegrationTests`) sit alongside.

### Plugin / Module System

`Common.Core.Loader` dynamically discovers `IModule` implementations at startup. Every `.Handlers` project exposes a module that:
- registers its own services (`ConfigureServices`)
- runs DB migrations (`OnStartup`)
- maps routes (`UseEndpoints`)

`Program.cs` in each host calls `Loader.Current.Compose()` — this is the entry point for understanding how all DI registrations and migrations occur without explicit references between host and handler projects.

### CQRS Flow

Controllers → `IHikyaku.Send(command/query)` → Handler in `.Handlers` → DbContext → Response.

Commands and queries live in `.Core/CQRS/`; handlers live in `.Handlers/CQRS/`. Never put business logic in controllers.

### Driver Matching Algorithm

`SearchBestDriverHandler` scores candidates:

```
score = (distanceWeight × normalizedDistance) + (ratingWeight × (1 − normalizedRating))
```

Results are intentionally **not** cached — driver location and availability change every few seconds, so a TTL-cached candidate list is either stale or constantly invalidated; the spatially-indexed query is both cheaper and more correct (see `DESIGN.md`). Location stored as `Point` (SRID 4326) via NetTopologySuite; SQL Server geospatial index is used for proximity queries.

### Real-time Flow

Ride service publishes events to RabbitMQ → Hub service consumes and pushes to connected SignalR clients. Key hub methods: `SendToRiderNewDriverLocation`, `SendToRiderRideAccepted`, `SendToRiderRideCompleted`, `SendToRiderNewRateReceived`.

### Shared Infrastructure (`Common/Common.Core`)

- **Loader**: plugin discovery and composition
- **Cache**: Redis abstraction (invalidated on location/status change)
- **Rate limiting**: per-policy limits (e.g. `driver_location_update`, `driver_registration`)

## Key Notes

- Uses **Newtonsoft.Json** on every wire path — HTTP, SignalR, and Kaido's RabbitMQ payloads — with geometry handled by NetTopologySuite's own `GeometryConverter` (`NetTopologySuite.IO.Converters`), not a hand-rolled one. The one exception is `Common.Core.Cache.RedisCacheService`, which serializes with `System.Text.Json` plus `GeoJsonConverterFactory` (`NetTopologySuite.IO.GeoJSON4STJ`). That converter is load-bearing, not decoration: plain STJ cannot write a `Point` at all (`Z`/`M` are `NaN`), so every cache write threw and was swallowed, leaving the cache permanently empty.
- Integration tests (Testcontainers: MsSql/RabbitMq/Redis) live in `Driver.IntegrationTests` and `Ride.IntegrationTests`. They cover four of the six concerns the other variants cover — the auth gate, nearest-driver search with its spatial index, the Redis round trip, and the ride lifecycle. **Cross-service dispatch over the broker and the ride-event-to-SignalR relay are not covered here, and cannot be**: both need two services running at once, and this variant cannot host two in one process. `Common.Core.Loader` is a process-wide singleton that composes from `Directory.GetCurrentDirectory()`, so co-hosted services share one module set — every module loads into every host, and `IHikyaku.Send` resolves locally instead of going to Kaido. That is a property of the plugin design, not a gap in the tests; the four variants that boot several hosts side by side cover those two concerns instead.
- `Ride.IntegrationTests` references `Driver.Handlers` on purpose: `RequestRide` sends `GetDriverStatus`, which a deployment routes to the Driver service. Putting the module's DLL in the test output makes the Loader compose it, which is exactly how this architecture decides what a process hosts — and is also why the broker hop is untestable from here.
- Three things about the harness are load-bearing:
  - **One container set per assembly.** Test classes join `IntegrationTestCollection` (`ICollectionFixture`), not `IClassFixture`. With `IClassFixture` each class got its own factory and therefore its own SQL Server/RabbitMQ/Redis. Never reintroduce `IClassFixture` here.
  - **`ResetDatabaseAsync` empties the tables, it does not recreate them.** `EnsureDeleted` + `EnsureCreated` rebuilds the schema from the EF model, which drops everything a migration does outside the model — `SIX_Drivers_LastLocation` (raw SQL in `20260731180239_AddDriverLastLocationSpatialIndex`) among it. `DatabaseResetTests` pins this. The reset itself lives in [`Commons/Voyager.TestInfra/DatabaseResetter.cs`](../Commons/Voyager.TestInfra/DatabaseResetter.cs), shared with every other variant — don't reimplement it here.
  - **The harness replaces the host's OpenIddict registration** and adds a test-only `connect/token` (`TestTokenController`), because the shipped validation targets a remote issuer a local test server cannot be. The production auth wiring is therefore *not* what these tests cover.
- `Common.Core.Loader` is a process-wide singleton but `Compose()` runs once per host boot, so it is locked and idempotent per directory/assembly/module type. Two `WebApplicationFactory` instances in one test process used to make it throw `Collection was modified` — see `Driver.Tests/LoaderTests.cs` before changing it.
- Unit tests use `UseInMemoryDatabase`. Handlers using a relational-only EF feature (`ExecuteUpdate`/`ExecuteDelete`, raw SQL) have no in-memory implementation and are **not unit-tested** — `UpdateUserRatingHandler` folds a rating into a running average with one atomic `ExecuteUpdateAsync` and carries no test, which is why `Identity.Handlers` sits well below the other assemblies in the coverage table. **Do not add a SQLite provider to close that gap**: SQLite is deliberately not a dependency of this repo, and reintroducing it will be rejected. This variant has no Identity integration suite either (only `Driver.IntegrationTests` and `Ride.IntegrationTests`), so the handler is currently proven by nothing but a pass against docker-compose.
- CI enforces an **80% line-coverage gate** on business logic (`{Service}.Core`, `{Service}.Handlers`, `Hub.API`) via `scripts/check-coverage.py`. `Common.Core` (cache, rate limiting, the AssemblyLoadContext plugin loader) is infra shared across this variant's own services — same "no domain logic" role as the root `Commons/` folder — so it's excluded from the gate, same as Api/Controllers and generated EF migrations/DbContext/`Module` (`IModule` registration)/`UserManagerService` (thin `UserManager<T>` pass-through). The coverage collector only sees assemblies actually loaded by a test process — a service with no `{Service}.Tests` project silently vanishes from the gate's denominator instead of dragging the score down. Every service must have one, or its real coverage (likely near 0%) never gets measured at all.
- Nullable reference types are **disabled** in every project, tests included. `.editorconfig` additionally sets CS8618/CS8632/CS8600 to `none`, so turning nullable on in one project will not produce the signal you expect until those suppressions are lifted too.
- `../.editorconfig` enforces 2-space indentation and LF line endings.
