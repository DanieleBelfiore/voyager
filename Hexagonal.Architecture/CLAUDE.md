# CLAUDE.md

Guidance for Claude Code when working inside this architecture variant. See [README.md](README.md) for the full rationale and [../CLAUDE.md](../CLAUDE.md) for portfolio-wide rules.

## Scope

All four services are implemented: Identity, Driver, Ride, Hub. Hub has no `Hub.Core.Domain` — pure relay, no entities.

## Commands

Run from this directory (`Hexagonal.Architecture/`):

```bash
docker-compose up -d                              # SQL Server, Redis, RabbitMQ, all 4 services
dotnet build Voyager.HexagonalArchitecture.sln
dotnet test Voyager.HexagonalArchitecture.sln
docker-compose down -v --rmi local
```

Ports: Hub 5200, Identity 5201, Driver 5202, Ride 5203 (offset from both other variants so all three stacks can run at once).

## The shape — don't collapse it back into Clean Architecture

`{Service}.Core` → entities (where relevant) + `Ports/Primary/` + `Ports/Secondary/` + `UseCases/`, all in one project. Don't split this back into separate Domain/Application projects — the single hexagon is the point of this variant.

**Every use case implements a primary port**: `IXxxUseCase : IRequestHandler<Command>` (or `<Command, Response>`), defined next to the command/query type itself in `Ports/Primary/`. The use case class implements that port. This is what makes it reachable two ways — directly injected locally, and discovered by Hikyaku's assembly scan for Kaido's remote dispatch — without any duplication. When adding a new use case:

1. Define the command/query type + `IXxxUseCase : IRequestHandler<...>` in `{Service}.Core/Ports/Primary/`. Skip the port interface only if nothing local will ever inject it directly (i.e. it's reachable exclusively via Kaido from another service) — see `GetActiveRideForHubUseCase` for the pattern.
2. Implement it in `{Service}.Core/UseCases/`, depending only on secondary ports.
3. If it needs a new capability, add the secondary port under `Ports/Secondary/` and implement it in `{Service}.Adapters.Secondary`.
4. Register the secondary adapter in `{Service}AdaptersExtensions.Add{Service}SecondaryAdapters`.
5. In `{Service}.Api`'s `Program.cs`, add `services.AddScoped<IXxxUseCase, XxxUseCase>()` (only needed if something local injects it) — Hikyaku's `RegisterServicesFromAssembly(coreAssembly)` handles the remote-dispatch registration regardless.
6. Inject the port directly into the controller/hub constructor and call `.Handle(...)`. **Never call `IHikyaku.Send(...)` from a controller in this variant** — that's Clean.Architecture's pattern, not this one. If you catch yourself adding `IHikyaku` to a controller's constructor, stop.

`{Service}.Adapters.Secondary` → implements every secondary port. Same "only layer that knows about EF Core/Kaido/Voyager.Shared" rule as Clean.Architecture's Infrastructure. **Exception**: Hub's `SignalRHubRelay` lives in `Hub.Api` (see README's "Why Hub has no Domain" section) — don't move it into `Hub.Adapters.Secondary`.

## Namespace collision gotcha

Every service's root namespace segment matches its aggregate's name (`Driver.Core.Domain.Driver`, `Ride.Core.Domain.Ride`) — the compiler reads a bare `Driver` or `Ride` inside those namespaces as the namespace segment, not the type, and fails with CS0118. Always alias: `using DriverEntity = Driver.Core.Domain.Driver;` / `using RideEntity = Ride.Core.Domain.Ride;`. `Identity`/`User` and `Hub` don't collide, no alias needed there.

## Cross-service calls

Same `Voyager.Contracts` + Kaido mechanism as every other variant — see [Commons/README.md](../Commons/README.md) and [Clean.Architecture/CLAUDE.md](../Clean.Architecture/CLAUDE.md)'s "unification rule". The wire contracts are shared across variants (Driver's `Voyager.Contracts.Driver.AddDriver` is the exact same type Clean.Architecture uses), so no new contracts were needed porting this variant — only the local plumbing around them changed.
