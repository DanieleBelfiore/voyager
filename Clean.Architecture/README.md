# Voyager — Clean Architecture

> Part of the [Voyager architecture portfolio](../README.md). This variant re-implements the same ride-sharing domain using **Clean (Onion) Architecture**: dependencies point inward only, and the domain model has zero knowledge of frameworks, databases, or the web.

All four services — Identity, Driver, Ride, Hub — are implemented.

## The dependency rule

```
{Service}.Domain          ← entities, enums. No package references except NetTopologySuite (a
                             geometry primitive, not a framework) where relevant, plus
                             Commons/Voyager.Errors in Ride.Domain only (two dependency-free
                             exception types thrown by its invariants). Hub has no
                             Domain project at all — see "Why Hub has no Domain layer" below.
      ↑
{Service}.Application      ← CQRS commands/queries/handlers, DTOs, Mapperly mapper, and the
                             *ports* (interfaces) Infrastructure must implement. May also
                             reference Commons/Voyager.Contracts (message shapes only — see
                             Commons/README.md), but nothing else outside itself and Domain.
      ↑
{Service}.Infrastructure   ← implements every port: EF Core repository + migrations, Redis
                             cache adapter, Kaido-based cross-service query/command/event
                             clients, config-bound settings.
      ↑
{Service}.Api               ← composition root. Wires Infrastructure implementations into
                             Application's ports, hosts ASP.NET Core controllers/hubs, owns
                             Program.cs, Dockerfile, appsettings.
```

Arrows point toward Domain. `{Service}.Application` never references `{Service}.Infrastructure`, EF Core, or ASP.NET Core — only the ports it declared (plus Voyager.Contracts message shapes). `{Service}.Domain` never references EF Core, Hikyaku, or ASP.NET Core at all.

## What's different from Plugin.Microservices.CQRS

| | Plugin.Microservices.CQRS | Clean.Architecture |
|---|---|---|
| Composition | Dynamic plugin `Loader` discovers `IModule`s at runtime via `AssemblyLoadContext` | Explicit composition root in each `Program.cs` — no runtime discovery |
| Persistence | Handlers talk to `I{Service}Context` (EF `DbContext` exposed via interface) directly | `I{Service}Repository` port in Application; EF Core is an Infrastructure implementation detail behind it |
| Domain model | Anemic — entities are property bags, business logic lives entirely in handlers | Rich — e.g. `Driver.UpdateAvailability()`, `Ride.Cancel()` enforce invariants on the entity itself |
| Persistence mapping | Data annotations (`[Table]`, `[Key]`, `[Index]`) directly on the model | `IEntityTypeConfiguration<T>` Fluent API in Infrastructure — Domain stays free of EF attributes |
| Cross-cutting deps (cache/ratings/config) | Injected directly (`ICacheService`, `IConfiguration`) into handlers | Behind Application-owned ports; Infrastructure adapts them |
| Identity's user model | `VoyagerUser : IdentityUser<Guid>`, ASP.NET Core Identity's `UserManager`/`SignInManager` | Plain `User` domain entity; password hashing behind an `IPasswordHasher` port (still backed by ASP.NET Core Identity's standalone `PasswordHasher<T>` — no reason to hand-roll crypto), OpenIddict token issuance stays in Api since it's inherently a protocol concern |

## Cross-service communication

Same mechanism as every variant in this portfolio: **Kaido** gives Hikyaku implicit remote dispatch over RabbitMQ — `IHikyaku.Send(request)` runs locally if a handler is registered, otherwise Kaido routes it to whichever service does, keyed by the request type's full name. Wire contracts live in `Commons/Voyager.Contracts` (shared, message-shape-only) rather than a direct project reference to another service's internals — see [Commons/README.md](../Commons/README.md).

**Unification pattern**: when a command/query is genuinely called both locally (by its owning service's own controller) and remotely (by another service), the *owning service's* Application handler implements the shared `Voyager.Contracts` type directly instead of maintaining a separate local type — one canonical type, works identically whether dispatched in-process or over the wire. Used for `Driver.AddDriver`, `Driver.UpdateLocation`, `Identity.UpdateUserRating`, `Identity.GetUsersRatings`. Where the remote caller needs a slimmer response than the local one (e.g. Hub only needs a ride's pickup point, not its full detail payload), the owning service adds a second, purpose-fit handler for the shared contract alongside its richer local one (`Ride.GetActiveRide`, `Ride.GetRideETA`) rather than forcing one shape to serve both.

Two deliberate corrections versus the Plugin variant, found while porting:

- **Driver's convenience passthrough, dropped.** The Plugin variant's `DriverController` also exposes `GetActiveRide`/`GetRideDriverHistory` as a passthrough to Ride's data. This variant drops that — Ride already owns those endpoints, and duplicating them here would leak a foreign bounded context into a controller that's supposed to depend on nothing but its own Application layer.
- **Ride→Hub real-time notifications, fixed.** The Plugin variant's Ride handlers inject `IHubContext<VoyagerHub, IVoyagerShareClient>` directly and call it in-process. But Ride and Hub are separate services with no SignalR backplane configured, so those calls only ever reach clients connected to Ride's own (client-less) SignalR endpoint — dead code. This variant publishes proper Hikyaku notifications instead (`Voyager.Contracts.Ride.RideAccepted`, `RideCancelled`, `RideCompleted`, `NewRideRequested`, `DriverRatingReceived`, `RiderRatingReceived`), which Kaido fans out to Hub — the service that actually owns connected SignalR clients — where they're relayed for real. See `Commons/Voyager.Contracts/Ride/RideEvents.cs`.

## Why Hub has no Domain layer

Hub is a pure real-time relay: it owns no persistent entities, just reacts to events from Ride and pushes state it queries from Driver/Ride. Giving it an empty `Hub.Domain` project for the sake of a uniform five-project shape would be cargo-culting the pattern rather than applying it — so this variant just has `Hub.Application` / `Hub.Infrastructure` / `Hub.Api`. `Hub.Application`'s `UpdateDriverLocationHandler` holds the one piece of real orchestration logic (update location → find active ride → push location + ETA → push arrival if within 500m), moved out of the SignalR `Hub` class itself so it's unit-testable without a SignalR test harness.

One layering exception, and it's structural, not a shortcut: `SignalRHubRelay` (the `IHubRelay` port implementation) lives in `Hub.Api`, not `Hub.Infrastructure`. `IHubContext<VoyagerHub, IVoyagerShareClient>` is generic over the concrete `Hub<TClient>` subclass, which is itself a presentation-layer type — putting the relay in Infrastructure would mean Infrastructure depends on Api, backwards. See the comment on `SignalRHubRelay.cs`.

## Testing

Application handlers are unit-tested directly against NSubstitute mocks of their ports (repository, cache, cross-service query/event clients) — no EF Core `InMemory` provider, no database, no message bus, no SignalR test harness. Once persistence/messaging/real-time is behind a port, the handler tests don't need any of that infrastructure to run. This is stricter than the Plugin variant's tests, which exercised a real `DbContext` against EF's InMemory provider. Identity and Hub have no equivalent tests in the Plugin variant at all — added here since testability without infrastructure is one of the things this style is meant to demonstrate.

## Running

```bash
cd Clean.Architecture
docker-compose up -d
dotnet run --project Demo/Demo.csproj
```

| Service | URL | Swagger |
|---|---|---|
| Hub | http://localhost:5100 | `/hub/swagger` |
| Identity | http://localhost:5101 | `/swagger` |
| Driver | http://localhost:5102 | `/driver/swagger` |
| Ride | http://localhost:5103 | `/ride/swagger` |

Ports and infra (`sqlserver` 1434, `redis` 6380, `rabbitmq` 5673/15673) are offset from the Plugin variant so both stacks can run side by side without colliding.

EF Core migrations live in each service's `Infrastructure/Persistence/Migrations` and run automatically on startup.

```bash
# Build / test
dotnet build Voyager.CleanArchitecture.sln
dotnet test Voyager.CleanArchitecture.sln
```
