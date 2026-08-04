# CLAUDE.md

Guidance for Claude Code when working inside this architecture variant. See [README.md](README.md) for the full rationale and [../CLAUDE.md](../CLAUDE.md) for portfolio-wide rules.

## Scope

All four modules are implemented: Identity, Driver, Ride, Hub, each `Modules/{Service}/{Service}.Module` + `Modules/{Service}/{Service}.Tests`, plus one `Host/` executable that composes all four. There is no per-module executable and no message bus anywhere in this variant — don't add Kaido/RabbitMQ back in.

## Commands

Run from this directory (`Modular.Monolith/`):

```bash
docker-compose up -d                          # SQL Server, Redis, the single app container
dotnet build Voyager.ModularMonolith.sln
dotnet test Voyager.ModularMonolith.sln
docker-compose down -v --rmi local
```

One port: 5400 (offset from the other three variants).

## The shape — don't collapse the module boundary

Each module is a separate class library project. **Default every new type to `internal`.** Only flip it to `public` when the C# compiler forces it via CS0050 ("inconsistent accessibility") — which happens exactly when a type is:
- A `[FromBody]` / route-bound parameter type on a public controller action, or
- A return type (including inside `ActionResult<T>`) of a public controller action.

Entities, `DbContext`s, Hikyaku command/query envelope types, handlers, and validators are **never** part of a public signature — they stay `internal`. If you're adding a new use case:

1. `Features/{UseCase}/` — same feature-folder shape as [Vertical.Slice.Architecture](../Vertical.Slice.Architecture/CLAUDE.md), but everything defaults to `internal`.
2. If the command needs a request-body DTO or the query needs a response DTO, those specific types are `public` (with a one-line comment noting why, matching the existing files) — everything else in the folder stays `internal`.
3. If it's a genuinely cross-service contract, implement `Voyager.Contracts.{Namespace}.{Type}` directly (the unification pattern, same as every other variant) rather than defining a new local type.
4. The controller is `public` (required for ASP.NET Core's controller discovery) and injects `IHikyaku` — same as [Vertical.Slice.Architecture](../Vertical.Slice.Architecture/README.md), local and cross-module dispatch both go through `.Send`/`.Publish`.
5. **Never add a repository, port, or message-bus client.** A handler needing another module's data sends that module's `Voyager.Contracts` request via `IHikyaku` directly — same call whether the target handler happens to live in the same assembly or not.

## Cross-module calls

`IHikyaku.Send`/`.Publish` with `Voyager.Contracts` types, registered once in `Host/Program.cs` (`AddHikyaku(cfg => cfg.RegisterServicesFromAssemblies(...))` scanning all four module assemblies plus `AddValidatorsFromAssemblies` for the shared `ValidationBehavior<,>` pipeline). There is no Kaido, no RabbitMQ, no adapter class translating a local call into a remote one — the mediator resolves the target handler directly out of the same DI container, regardless of which module assembly it's declared in. See [Commons/README.md](../Commons/README.md) for why `Voyager.Contracts` is still the right place for these shapes even without a wire format.

## New module → new project, new `InternalsVisibleTo`

If you ever add a fifth module: give it its own class library project (`{Service}.Module`) plus `{Service}.Tests`, add a project-level `AssemblyInfo.cs` with `[assembly: InternalsVisibleTo("{Service}.Tests")]`, expose exactly one public `DependencyInjection/{Service}ModuleExtensions.cs` (an `Add{Service}Module` for DI registration, and `Map{Service}Module`/`Use{Service}Module` for pipeline/endpoint wiring if it has any), and reference it from `Host/Host.csproj`. Host must never reference a module's internal types directly — if you find yourself doing that, the module boundary has already been broken.

If a test needs to mock an `internal` type or interface (NSubstitute/Moq, via Castle DynamicProxy), also add `[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]` to that module — see `Hub.Module/AssemblyInfo.cs` for the existing example (needed to mock `IHubContext<VoyagerHub, IVoyagerShareClient>`, since `VoyagerHub` is internal).

## Persistence

Each module keeps its own `DbContext` and its own database (`identity`, `driver`, `ride`) — never let two modules' `DbContext`s point at the same connection string, and never query another module's tables directly even though nothing would stop you at the SQL level. The only sanctioned cross-module read is `IHikyaku.Send` into that module's own query handler.

## Namespace collision gotcha

Same issue as every other variant, same fix: `Driver.Module.Entities.Driver` and `Ride.Module.Entities.Ride` collide with their own root namespace segment — alias with `using DriverEntity = Driver.Module.Entities.Driver;` / `using RideEntity = Ride.Module.Entities.Ride;`.

## Testing

Same shape as [Vertical.Slice.Architecture](../Vertical.Slice.Architecture/CLAUDE.md#testing): construct the module's `DbContext` against `UseInMemoryDatabase(Guid.NewGuid().ToString())`, seed it, call `handler.Handle(...)`, and mock `IHikyaku`/`IHubContext<...>` only where the handler actually depends on them.

**Exception — relational-only features need a relational provider.** `ExecuteUpdate`/`ExecuteDelete` and raw SQL have no in-memory implementation and throw `InvalidOperationException` under `UseInMemoryDatabase`. Those tests open a `SqliteConnection("DataSource=:memory:")`, hold it open for the fixture's lifetime, and call `Database.EnsureCreated()` — see `Identity.Tests/Features/UpdateUserRatingHandlerTests.cs`. InMemory stays the default everywhere else, and SQLite proves the logic, not SQL Server's own translation of it.

## Integration tests

`Host.IntegrationTests` boots the one host against Testcontainers (SQL Server, RabbitMQ, Redis) —
RabbitMQ only because the shared containers fixture starts it; this variant has no bus. Six
concerns, one test class each: the auth gate, nearest-driver search, the Redis round trip,
cross-module dispatch, a ride event reaching a SignalR client, and the ride lifecycle. Deliberately
the same six as the service-per-process variants, so the suites line up.

```bash
dotnet test Host.IntegrationTests/Host.IntegrationTests.csproj    # needs Docker
```

Modules are `internal`, so these tests reach the system only over HTTP — no seeding a `DbContext`
directly. That is the right constraint here, not a limitation to work around.

Two things to know before changing the harness — see [`Commons/Voyager.TestInfra`](../Commons/Voyager.TestInfra):

- **The SignalR test builds its own host.** A long-polling connection holds a request open on the
  in-memory test server for as long as it lives; on the host every other test drives, that pins the
  server and the suite stops making progress.
- **Hosts run as `Production`.** In `Development` the developer exception page answers 500 for
  everything and hides the ProblemDetails mapping that turns `KeyNotFoundException` into 404.

Configuration reaches the host as environment variables rather than through
`ConfigureAppConfiguration`: sources added there are merged when the host is built, too late for
anything binding configuration eagerly at registration (`AddRedisCache` does — which is why the
cache silently pointed at the compose hostname until this was fixed).
