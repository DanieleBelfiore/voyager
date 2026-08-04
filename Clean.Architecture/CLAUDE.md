# CLAUDE.md

Guidance for Claude Code when working inside this architecture variant. See [README.md](README.md) for the full rationale and [../CLAUDE.md](../CLAUDE.md) for portfolio-wide rules.

## Scope

All four services are implemented: Identity, Driver, Ride, Hub. Hub has no `Hub.Domain` project by design — it's a pure relay with no entities of its own; see README.md's "Why Hub has no Domain layer".

## Commands

Run from this directory (`Clean.Architecture/`):

```bash
docker-compose up -d                              # SQL Server, Redis, RabbitMQ, all 4 services
dotnet build Voyager.CleanArchitecture.sln
dotnet test Voyager.CleanArchitecture.sln
dotnet test Driver/Driver.Tests/Driver.Tests.csproj --filter "FullyQualifiedName~AddDriverHandlerTests"
docker-compose down -v --rmi local
```

Ports (offset from Plugin.Microservices.CQRS so both can run at once): Hub 5100, Identity 5101, Driver 5102, Ride 5103.

## The dependency rule — don't break it

`{Service}.Domain` → referenced by nothing, references nothing but NetTopologySuite where relevant. (Hub has none — don't create one just to match the other services' shape.)
`{Service}.Application` → references only `{Service}.Domain` and `Commons/Voyager.Contracts` (pure message shapes for commands/queries/events that cross a service boundary — no logic, so referencing it isn't a layering violation, same category as depending on a DTO/schema package). Never add a reference to `{Service}.Infrastructure`, EF Core, ASP.NET Core, or `Commons/Voyager.Shared` from here — if a capability is needed (cache, cross-service query, config value) that isn't just a message shape, add a port interface under `{Service}.Application/Ports/` instead and implement it in Infrastructure.
`{Service}.Infrastructure` → implements Application's ports. This is the only layer allowed to reference EF Core, Kaido/Hikyaku transport concerns, and `Commons/Voyager.Shared`. **Exception**: Hub's `SignalRHubRelay` (implements `IHubRelay`) lives in `Hub.Api`, not `Hub.Infrastructure`, because `IHubContext<VoyagerHub, IVoyagerShareClient>` is generic over the concrete Hub class — a presentation type. Don't move it to Infrastructure; that would make Infrastructure depend on Api.
`{Service}.Api` → composition root only. Controllers/hubs call `IHikyaku.Send(...)`/`Publish(...)`, nothing else; DI wiring lives in `{Service}.Infrastructure/DependencyInjection/*Extensions.cs` and is invoked from `Program.cs`.

If you're about to add a `using {Service}.Infrastructure` inside `{Service}.Application`, stop — that's the one direction this whole variant exists to demonstrate you don't do.

## Persistence mapping

EF Core configuration is Fluent API only (`{Service}.Infrastructure/Persistence/Configurations/*Configuration.cs`), not data annotations on the entity. Keep it that way — domain entities should never gain an `[EntityFrameworkCore]`-namespace attribute.

## Adding a new command/query

1. Command/query record + handler in `{Service}.Application/CQRS/{Commands,Queries}/`, depending only on ports.
2. If it needs a new capability, add the port under `{Service}.Application/Ports/` first.
3. Implement the port in `{Service}.Infrastructure`.
4. Register the implementation in `{Service}InfrastructureExtensions.Add{Service}Infrastructure`.
5. Wire the controller/hub action in `{Service}.Api`.

## Cross-service calls

Use the shared wire contract from `Commons/Voyager.Contracts` (not another service's Application/Domain project) and send/publish it via the local `IHikyaku` — Kaido routes it remotely if no local handler exists. See `Driver.Infrastructure/Messaging/RemoteRatingsQueryService.cs` (request/response) or `Ride.Infrastructure/Messaging/RemoteRideEventPublisher.cs` (notifications/events) for the pattern.

**Unification rule**: if a command/query needs to be called both locally (by its owning service) and remotely (by another service), the owning service's Application handler should implement the `Voyager.Contracts` type directly rather than keeping a separate local type — see `AddDriver`, `UpdateLocation` (Driver), `UpdateUserRating`, `GetUsersRatings` (Identity) for the pattern. If the remote caller needs a different (usually slimmer) response shape than local callers, add a second handler for the shared contract rather than compromising the local one — see `GetActiveRide`/`GetRideETA` (Ride), which have both a rich local handler and a slim one for Hub.

## Integration tests

`Voyager.IntegrationTests` boots all four services in one test process against Testcontainers
(SQL Server, RabbitMQ, Redis) — the variant's real topology, since most of what distinguishes it
from the modular monolith is what happens *between* services. Six concerns, one test class each:
the auth gate, nearest-driver search, the Redis round trip, cross-service dispatch, a ride event
reaching a SignalR client, and the ride lifecycle.

```bash
dotnet test Voyager.IntegrationTests/Voyager.IntegrationTests.csproj    # needs Docker
```

Three things about the harness are load-bearing — see [`Commons/Voyager.TestInfra`](../Commons/Voyager.TestInfra):

- **Each host is behind an `extern alias`.** Four top-level-statement hosts each contribute a
  `Program` to the global namespace, so referencing them from one assembly needs aliases.
- **Driver/Ride/Hub validate tokens against Identity over HTTP**, and nothing listens on a real
  socket under `WebApplicationFactory`. Their outbound HTTP is redirected to Identity's test server
  with a `DelegatingHandler` — OpenIddict rejects a non-`HttpClientHandler` primary handler
  outright, so it cannot be swapped at the primary. The shipped `SetIssuer` + `UseSystemNetHttp`
  wiring stays exactly as deployed.
- **Hosts run as `Production`.** In `Development` the pipeline installs the developer exception
  page, which answers 500 for everything and hides the ProblemDetails mapping that turns
  `KeyNotFoundException` into 404.

Configuration reaches the hosts as environment variables rather than through
`ConfigureAppConfiguration`: sources added there are merged when the host is built, which is too
late for anything binding configuration eagerly at registration (`AddRedisCache` does).
