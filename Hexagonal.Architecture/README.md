# Voyager — Hexagonal Architecture

> Part of the [Voyager architecture portfolio](../README.md). This variant re-implements the same ride-sharing domain using **Hexagonal Architecture (Ports & Adapters)**: the domain core defines the ports it needs, and everything else — HTTP, SignalR, EF Core, Redis, the message bus — plugs in from the outside as an adapter.

All four services — Identity, Driver, Ride, Hub — are implemented.

## The shape

```
{Service}.Core                     ← the hexagon: entities (where relevant — see Hub below),
                                      primary ports, secondary ports, and use cases, all together.
                                      Unlike Clean.Architecture's Domain/Application split, this
                                      variant treats "the application core" as one thing — that's
                                      the point of hexagonal: one hexagon, ports on every side.
      ↑ implements
{Service}.Adapters.Secondary       ← secondary adapters: EF Core repository + migrations, Redis
                                      cache, Kaido-based cross-service clients. Symmetric with
                                      the primary side below — both are just adapters plugged
                                      into the same ports.
      ↑ implements / injects
{Service}.Api                       ← primary adapter + composition root: REST
                                      controllers (Driver, Identity, Ride) or a SignalR hub
                                      (Hub) call use cases directly through their primary port —
                                      no mediator indirection for local calls.
```

## The defining difference: how a use case is reached

Every use case implements a **primary port** shaped like `IXxxUseCase : IRequestHandler<Command>` (or `<Command, Response>`). That single interface makes the use case reachable two different ways at once, and neither one is aware of the other:

1. **Locally** — `Driver.Api`'s `DriversController` takes `IAddDriverUseCase` as a constructor dependency and calls `.Handle(command, ct)` directly. No `IHikyaku.Send`, no dispatch layer — just an interface call, the same way you'd inject any other collaborator.
2. **Remotely** — `AddDriverUseCase` is *also* discovered by Hikyaku's assembly scan (`RegisterServicesFromAssembly(coreAssembly)`), which is what makes it a valid Kaido target: when Identity calls the shared `Voyager.Contracts.Driver.AddDriver` command and no local handler exists in Identity's own process, Kaido routes it over RabbitMQ to Driver, where Hikyaku resolves the same `AddDriverUseCase` instance and invokes the identical `Handle` method.

Compare this to [Clean.Architecture](../Clean.Architecture/README.md), where **every** call — local or remote — goes through `IHikyaku.Send`. Here, the primary port *is* the seam: whether the caller is a REST controller in the same process or Kaido resolving a remote request, they're both just "something that drives the hexagon," and the hexagon doesn't need to know which.

A handful of use cases exist purely to serve a *remote* primary adapter and are never called locally — e.g. `GetActiveRideForHubUseCase`, which only Hub calls, over the wire, to find which ride group to notify. Those implement `IRequestHandler<T>` directly with no dedicated `IXxxUseCase` interface, since nothing local ever needs to inject one.

## What's different from Clean.Architecture

| | Clean.Architecture | Hexagonal.Architecture |
|---|---|---|
| Core structure | `Domain` and `Application` are separate projects (onion layers) | `Core` is one project — entities, ports, and use cases together (one hexagon) |
| Local dispatch | Controllers call `IHikyaku.Send(command)` | Controllers inject the use case's primary port directly and call `.Handle(...)` |
| Ports | Application defines only *secondary* ports (repository, cache, etc.) | Core defines both *primary* ports (`IXxxUseCase : IRequestHandler<T>`) and *secondary* ports, named accordingly |
| Persistence/messaging adapters | One `Infrastructure` project | `Adapters.Secondary` — same content, named to make the ports-and-adapters framing explicit |
| Vocabulary | Domain/Application/Infrastructure | Core/Ports.Primary/Ports.Secondary/Adapters.Secondary — hexagonal's own terms, not relabeled Clean Architecture |

Everything else — the rich domain entities enforcing their own invariants, EF Core configuration via Fluent API only, the Ride→Hub event-based fix, the `AddDriver`/`UpdateLocation`/`UpdateUserRating`/`GetUsersRatings` contract-unification pattern, the dropped Driver→Ride passthrough — carries over unchanged from Clean.Architecture; see that variant's README for the reasoning.

## Why Hub has no Domain / entities

Same reasoning as the Clean.Architecture variant: Hub is a pure real-time relay with no persistent state of its own. `Hub.Core` has ports and use cases but no `Domain` folder.

One layering exception, structural not a shortcut: `SignalRHubRelay` (implements `IHubRelay`) lives in `Hub.Api`, not `Hub.Adapters.Secondary`, because `IHubContext<VoyagerHub, IVoyagerShareClient>` is generic over the concrete `Hub<TClient>` subclass — a presentation type. Putting the relay in the secondary-adapters project would mean that project depends on Api, backwards.

## Testing

Unit tests instantiate use case classes directly — `new AddDriverUseCase(mockRepository)` — and call `.Handle(...)`, exactly the way a primary adapter would. No EF Core, no message bus, no SignalR, no Hikyaku pipeline involved in the test at all; the primary port *is* the seam the test exploits.

## Running

```bash
cd Hexagonal.Architecture
docker-compose up -d
dotnet run --project Demo/Demo.csproj
```

| Service | URL | Swagger |
|---|---|---|
| Hub | http://localhost:5200 | `/hub/swagger` |
| Identity | http://localhost:5201 | `/swagger` |
| Driver | http://localhost:5202 | `/driver/swagger` |
| Ride | http://localhost:5203 | `/ride/swagger` |

Ports and infra (`sqlserver` 1435, `redis` 6381, `rabbitmq` 5674/15674) are offset from both other variants so all three stacks can run side by side without colliding.

```bash
# Build / test
dotnet build Voyager.HexagonalArchitecture.sln
dotnet test Voyager.HexagonalArchitecture.sln
```
