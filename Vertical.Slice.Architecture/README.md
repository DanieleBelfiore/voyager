# Voyager — Vertical Slice Architecture

> Part of the [Voyager architecture portfolio](../README.md). This variant re-implements the same ride-sharing domain using **Vertical Slice Architecture**: instead of horizontal layers (Domain/Application/Infrastructure) or ports-and-adapters, each use case is a self-contained **feature folder** holding everything it needs — request, handler, validator, controller — and touches infrastructure (EF Core, cache, IHikyaku) directly. No repository, no port interfaces, no adapters project.

All four services — Identity, Driver, Ride, Hub — are implemented.

## The shape

```
{Service}.Api
├── Entities/           ← the aggregate root(s) — the one thing every feature shares
├── Persistence/         ← DbContext, EF configurations, migrations — also shared
├── Shared/               ← cross-feature response shapes / helpers (kept minimal — see below)
└── Features/
    ├── AddDriver/
    │   ├── AddDriverHandler.cs        ← IRequestHandler<T>, injects DbContext/IHikyaku directly
    │   ├── AddDriverValidator.cs      ← FluentValidation, runs via a Hikyaku pipeline behavior
    │   └── AddDriverController.cs     ← one controller per feature, injects IHikyaku, calls .Send
    ├── UpdateLocation/
    │   └── ...
    └── ...                            ← one folder per use case, all siblings, no shared layer
```

Two projects per service — `{Service}.Api` and `{Service}.Tests` — versus Clean Architecture's five and Hexagonal's four. There's no `Domain`/`Application`/`Infrastructure` split and no `Core`/`Adapters.Secondary` split: **the feature folder is the unit of organization**, not the technical layer.

## What's genuinely different here, not just relabeled

Compare this to [Hexagonal.Architecture](../Hexagonal.Architecture/README.md), which still has secondary *ports* (`IDriverRepository`, `ICacheService`, `IRideEventPublisher`, …) — interfaces the Core defines and Adapters.Secondary implements. This variant has **none of that**:

- **No repository.** Every handler takes `{Service}DbContext` as a constructor dependency and queries it directly (`db.Drivers.FirstOrDefaultAsync(...)`). There's nothing to mock in a unit test except the `DbContext` itself, so tests use EF Core's InMemory provider instead of a hand-rolled fake or an NSubstitute mock.
- **No event-publisher/cross-service-query port.** A handler that needs to notify Hub, ask Identity for ratings, or ask Driver for a location just calls `mediator.Send(...)` / `mediator.Publish(...)` directly — the exact same `IHikyaku` Kaido already uses for remote dispatch. There's no `IRideEventPublisher` wrapping that call.
- **No `ICacheService` port** in Driver — handlers inject `Voyager.Shared.Cache.ICacheService` directly (that interface is already infra-agnostic, owned by Commons, so wrapping it again would be pure ceremony).
- **Validation moved into the pipeline.** A `ValidationBehavior<TRequest,TResponse>` (in `Commons/Voyager.Shared/Validation`) runs every registered FluentValidation validator before a handler executes. `RegisterUser`, `UpdateLocation`, `UpdateAvailability` all have their own colocated `*Validator.cs`. Plugin, Clean and Hexagonal all do input validation with manual guard clauses inline in the handler; this variant introduced the Hikyaku-pipeline-behavior pattern instead, because it's the natural fit for feature folders — the validator lives right next to the request it validates. [Modular.Monolith](../Modular.Monolith/README.md) later adopted the same behavior (its own concession: every validator there is `internal`, so its `Host` must pass `includeInternalTypes: true` to `AddValidatorsFromAssemblies` or the behavior silently validates nothing).
- **One controller per feature**, not one controller per resource. `Driver.Api/Features/AddDriver/AddDriverController.cs`, `UpdateLocation/UpdateLocationController.cs`, etc. — each a few lines, injecting `IHikyaku` and calling `.Send`. This is what makes the folder genuinely self-contained: nothing about a feature lives outside its own folder except the aggregate entity and the DbContext, both of which are shared by construction (every feature operates on the same table).

## Cross-service dispatch — identical mechanism, simpler plumbing

Local and remote dispatch both go through `IHikyaku`, exactly like [Clean.Architecture](../Clean.Architecture/README.md) — a controller calls `mediator.Send(command)`, and if no local handler is registered for that exact type, Kaido routes it over RabbitMQ to whichever service does register one. The contract-unification pattern is unchanged: `AddDriver`, `UpdateLocation`, `UpdateUserRating`, `GetUsersRatings` are still the literal `Voyager.Contracts.*` types, handled directly with no local wrapper type. What's different is that in this variant there's no adapter class in between — a handler that needs a driver's rating just does `await mediator.Send(new GetUsersRatings { UserIds = [id] })` inline, because there's no port to hide it behind.

## `Shared/`

Kept deliberately small — this is the one concession to "some things really are cross-feature":

- **Response DTOs used by more than one feature** (`Ride.Api/Shared/RideDetailsResponse.cs`, read by `GetRideDetails`, `RequestRide`, `GetRideHistory` and `GetRideDriverHistory`) live here instead of being duplicated four times or forcing one feature to depend on another's folder.
- **Cross-feature pure logic** (`Ride.Api/Shared/RideEtaCalculator.cs`, used by both `GetRideETA` and the remote-only `GetRideETAForHub`) lives here too.
- **The concrete SignalR hub** (`Hub.Api/Shared/VoyagerHub.cs` + `IVoyagerShareClient.cs`) lives here because `IHubContext<VoyagerHub, IVoyagerShareClient>` is injected by *both* `UpdateDriverLocation`'s handler and every handler in `RideEvents` — it's shared infrastructure, not one feature's private concern.

Anything that's genuinely single-feature stays inside that feature's own folder, including small per-feature request DTOs (`UpdateAvailabilityRequest`, `RateRideRequest`, `RateDriverRequest`, …) even where two features have near-identical shapes — duplication is preferred over introducing a cross-feature dependency for a two-line class.

## Remote-only features

Same pattern as Hexagonal: `GetActiveRideForHub` and `GetRideETAForHub` in Ride have a handler but no controller — they're reached exclusively through Kaido's remote Hikyaku dispatch, driven by another service. Their request types live in `Voyager.Contracts.Ride`, which is what makes them constructible from another service.

`GetRideDriverHistory` is a third handler with no controller, but it is **not** remotely dispatched and is currently unreachable: its request type lives in `Ride.Api`'s own assembly, so no other service can reference it, and `Ride.Api/Program.cs`'s `InferLocalRequests([apiAssembly])` marks it local-only. A pre-existing gap inherited from the original Plugin implementation, not something introduced here.

## Namespace collision gotcha

Same issue as Hexagonal, same fix: each service's root namespace segment matches its aggregate's name (`Driver.Api.Entities.Driver`, `Ride.Api.Entities.Ride`), so a bare `Driver` or `Ride` referenced from a namespace starting with `Driver.`/`Ride.` resolves to the namespace segment, not the type (CS0118). Fixed with explicit aliases: `using DriverEntity = Driver.Api.Entities.Driver;` / `using RideEntity = Ride.Api.Entities.Ride;`, applied everywhere those entities are referenced outside their own file. The same applies to any shared contract type whose simple name matches a feature folder's name (e.g. `AddDriver` the contract vs. the `Features/AddDriver/` folder) — those are referenced fully qualified (`Voyager.Contracts.Driver.AddDriver`) rather than aliased, since they're only used once or twice per file.

## Testing

Unit tests spin up `{Service}DbContext` against EF Core's `UseInMemoryDatabase`, seed it directly, and call `handler.Handle(...)` — no repository to mock. Cross-service calls (`IHikyaku.Send`/`.Publish`) and `IHubContext` are mocked with NSubstitute where a handler depends on them. This is the most "integration-shaped" unit testing of any variant in the portfolio, because there's genuinely less to isolate — the whole point of dropping the repository port is that EF Core's InMemory provider *is* the seam now.

## Running

```bash
cd Vertical.Slice.Architecture
docker-compose up -d
dotnet run --project Demo/Demo.csproj
```

| Service | URL | Swagger |
|---|---|---|
| Hub | http://localhost:5300 | `/hub/swagger` |
| Identity | http://localhost:5301 | `/swagger` |
| Driver | http://localhost:5302 | `/driver/swagger` |
| Ride | http://localhost:5303 | `/ride/swagger` |

Ports and infra (`sqlserver` 1436, `redis` 6382, `rabbitmq` 5675/15675) are offset from the other three variants so every stack can run side by side without colliding.

```bash
# Build / test
dotnet build Voyager.VerticalSliceArchitecture.sln
dotnet test Voyager.VerticalSliceArchitecture.sln
```
