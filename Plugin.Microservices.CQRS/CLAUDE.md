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

**Voyager** is a ride-sharing backend built as four independent microservices on .NET 9, communicating via RabbitMQ and exposing real-time events through SignalR.

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
{Service}.API/          → Controllers (dispatch to MediatR)
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

Controllers → `IMediator.Send(command/query)` → Handler in `.Handlers` → DbContext → Response.

Commands and queries live in `.Core/CQRS/`; handlers live in `.Handlers/CQRS/`. Never put business logic in controllers.

### Driver Matching Algorithm

`SearchBestDriverHandler` scores candidates:

```
score = (distanceWeight × normalizedDistance) + (ratingWeight × (1 − normalizedRating))
```

Results are cached in Redis. Location stored as `Point` (SRID 4326) via NetTopologySuite; SQL Server geospatial index is used for proximity queries.

### Real-time Flow

Ride service publishes events to RabbitMQ → Hub service consumes and pushes to connected SignalR clients. Key hub methods: `SendToRiderNewDriverLocation`, `SendToRiderRideAccepted`, `SendToRiderRideCompleted`, `SendToRiderNewRateReceived`.

### Shared Infrastructure (`Common/Common.Core`)

- **Loader**: plugin discovery and composition
- **Cache**: Redis abstraction (invalidated on location/status change)
- **Rate limiting**: per-policy limits (e.g. `driver_location_update`, `driver_registration`)

## Key Notes

- Uses **Newtonsoft.Json** on every wire path — HTTP, SignalR, and Arbitrer's RabbitMQ payloads — with geometry handled by NetTopologySuite's own `GeometryConverter` (`NetTopologySuite.IO.Converters`), not a hand-rolled one. The one exception is `Common.Core.Cache.RedisCacheService`, which serializes with `System.Text.Json` plus `GeoJsonConverterFactory` (`NetTopologySuite.IO.GeoJSON4STJ`). That converter is load-bearing, not decoration: plain STJ cannot write a `Point` at all (`Z`/`M` are `NaN`), so every cache write threw and was swallowed, leaving the cache permanently empty.
- Integration tests (Testcontainers: MsSql/RabbitMq/Redis) exist for Driver and Ride only, and are excluded from CI (`--filter "FullyQualifiedName!~IntegrationTests"`) — run them locally.
- Unit tests use `UseInMemoryDatabase`, **except** where the handler under test uses a relational-only EF feature (`ExecuteUpdate`/`ExecuteDelete`, raw SQL), which InMemory does not implement. Those open a `SqliteConnection("DataSource=:memory:")` held open for the fixture's lifetime plus `Database.EnsureCreated()` — see `Identity.Tests/Handlers/Commands/UpdateUserRatingHandlerTests.cs`. SQLite proves the logic and the atomicity, not SQL Server's own translation, so those fixes still want a pass against docker-compose.
- CI enforces an **80% line-coverage gate** on business logic (`{Service}.Core`, `{Service}.Handlers`, `Hub.API`) via `scripts/check-coverage.py`. `Common.Core` (cache, rate limiting, the AssemblyLoadContext plugin loader) is infra shared across this variant's own services — same "no domain logic" role as the root `Commons/` folder — so it's excluded from the gate, same as Api/Controllers and generated EF migrations/DbContext/`Module` (`IModule` registration)/`UserManagerService` (thin `UserManager<T>` pass-through). The coverage collector only sees assemblies actually loaded by a test process — a service with no `{Service}.Tests` project silently vanishes from the gate's denominator instead of dragging the score down. Every service must have one, or its real coverage (likely near 0%) never gets measured at all.
- Nullable reference types are **disabled** in main projects, enabled only in test projects.
- `../.editorconfig` enforces 2-space indentation and LF line endings.
