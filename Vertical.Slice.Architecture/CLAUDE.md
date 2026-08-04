# CLAUDE.md

Guidance for Claude Code when working inside this architecture variant. See [README.md](README.md) for the full rationale and [../CLAUDE.md](../CLAUDE.md) for portfolio-wide rules.

## Scope

All four services are implemented: Identity, Driver, Ride, Hub. Each is exactly two projects — `{Service}.Api` and `{Service}.Tests`. There is no `Core`, `Domain`, `Application`, `Infrastructure` or `Adapters.*` project anywhere in this variant — don't create one.

## Commands

Run from this directory (`Vertical.Slice.Architecture/`):

```bash
docker-compose up -d                                     # SQL Server, Redis, RabbitMQ, all 4 services
dotnet build Voyager.VerticalSliceArchitecture.sln
dotnet test Voyager.VerticalSliceArchitecture.sln
docker-compose down -v --rmi local
```

Ports: Hub 5300, Identity 5301, Driver 5302, Ride 5303 (offset from all three other variants so every stack can run at once).

## The shape — don't collapse it back into Clean or Hexagonal

`{Service}.Api/Features/{UseCase}/` holds everything for that use case: the Hikyaku request, its handler, its validator (if any), and its controller. When adding a new use case:

1. Create `{Service}.Api/Features/{UseCase}/`.
2. Define the request type — `{UseCase} : IRequest` or `IRequest<TResponse>` — in that folder. If it's a cross-service contract that's also called remotely, implement `Voyager.Contracts.{Namespace}.{Type}` directly instead of defining a new local type (the unification pattern — see Clean.Architecture's CLAUDE.md for the full rationale).
3. Implement `{UseCase}Handler : IRequestHandler<...>` in the same folder. It takes `{Service}DbContext`, `Voyager.Shared.Cache.ICacheService`, and/or `IHikyaku` directly as constructor dependencies — **never introduce a repository, port, or adapter interface**. If you catch yourself writing `I{Something}Repository` or `I{Something}Service` as an abstraction over EF Core/cache/cross-service calls, stop — that's Clean or Hexagonal's pattern, not this one.
4. If the request needs format validation, add `{UseCase}Validator : AbstractValidator<{UseCase}>` in the same folder. It's picked up automatically — `Program.cs` registers `ValidationBehavior<,>` (from `Voyager.Shared.Validation`) as a Hikyaku pipeline behavior and calls `AddValidatorsFromAssembly` once per service.
5. If it needs an HTTP endpoint, add `{UseCase}Controller` in the same folder — inject `IHikyaku`, call `.Send(...)`. One controller per feature, not one controller per resource. Remote-only use cases (driven exclusively by another service via Kaido) get no controller at all — see `Ride.Api/Features/GetActiveRideForHub` for the pattern.
6. Only add something to `Shared/` if a second feature genuinely needs it (a response DTO, a pure calculation, or — for Hub — the concrete SignalR hub type). Default to duplicating a small DTO across two feature folders rather than creating a cross-feature dependency.

## Namespace collision gotcha

Every service's root namespace segment matches its aggregate's name (`Driver.Api.Entities.Driver`, `Ride.Api.Entities.Ride`) — the compiler reads a bare `Driver` or `Ride` inside those namespaces as the namespace segment, not the type, and fails with CS0118. Always alias: `using DriverEntity = Driver.Api.Entities.Driver;` / `using RideEntity = Ride.Api.Entities.Ride;`. The same applies to any shared contract type whose simple name matches its own feature folder (e.g. `Voyager.Contracts.Driver.AddDriver` referenced from `Features/AddDriver/`) — reference those fully qualified rather than aliasing, since they're only used once or twice per file. `Identity`/`User` and `Hub`'s `VoyagerHub : Hub<T>` don't collide, no alias needed there.

## Cross-service calls

Same `Voyager.Contracts` + Kaido mechanism as every other variant — see [Commons/README.md](../Commons/README.md) and [Clean.Architecture/CLAUDE.md](../Clean.Architecture/CLAUDE.md)'s "unification rule". The difference here is purely about what sits between the handler and `IHikyaku.Send`/`.Publish`: nothing. No `RemoteXxxService` adapter class — the handler calls `mediator.Send(...)` itself.

## Testing

No repository to mock — tests construct `{Service}DbContext` against `UseInMemoryDatabase(Guid.NewGuid().ToString())`, seed it directly, and call `handler.Handle(...)`. Mock `IHikyaku`/`IHubContext<VoyagerHub, IVoyagerShareClient>` with NSubstitute only where a handler actually depends on them (cross-service calls, event publishing, SignalR push).

**Exception — relational-only features need a relational provider.** `ExecuteUpdate`/`ExecuteDelete`, raw SQL, and anything else with no in-memory implementation throw `InvalidOperationException` ("not supported by the current database provider") under `UseInMemoryDatabase`. Those tests open a `SqliteConnection("DataSource=:memory:")`, keep it open for the fixture's lifetime, and call `Database.EnsureCreated()` — see `Identity.Tests/Features/UpdateUserRatingHandlerTests.cs`, where the handler folds a rating into a running average with one atomic `ExecuteUpdateAsync` so concurrent ratings can't lose each other.

Reach for SQLite only when the handler under test genuinely needs it. InMemory stays the default everywhere else — it's faster and needs no schema. Note what SQLite does *not* buy you: it proves the logic and the atomicity, not that SQL Server translates the same expression, so a relational-behaviour fix still deserves a pass against the real docker-compose stack.
