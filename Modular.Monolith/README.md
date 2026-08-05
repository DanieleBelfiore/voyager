# Voyager — Modular Monolith

> Part of the [Voyager architecture portfolio](../README.md). This variant re-implements the same ride-sharing domain as a **Modular Monolith**: the same four bounded contexts (Identity, Driver, Ride, Hub), but deployed as a single process on a single port. Module boundaries are enforced by C# visibility (`internal`) at compile time, not by a network hop — and there is no message bus, because there's nothing remote to route to.

All four modules are implemented.

## The shape

```
Modular.Monolith/
├── Host/                          ← the only executable. Composes every module, owns the one
│                                     Kestrel port. Never references a module's internal types —
│                                     only each module's public Add*Module/Map*Module methods.
└── Modules/
    ├── Identity/Identity.Module/  ← internal Entities/Persistence/handlers, public controllers
    ├── Driver/Driver.Module/         and DTOs that cross HTTP, public DependencyInjection/
    ├── Ride/Ride.Module/             extension class. Each module also has its own Tests
    └── Hub/Hub.Module/               project, granted access via InternalsVisibleTo.
```

Each module is a **separate class library project** — that's what makes the boundary real. If `Ride.Module` tried to reference `Driver.Module`'s `Driver` entity or `DriverDbContext` directly, it wouldn't compile: those types are `internal`, and internal means invisible outside the declaring assembly, full stop. The only way one module reaches another is through `Voyager.Contracts` + `IHikyaku` — the exact same contracts every other variant in this portfolio uses for cross-service calls, just dispatched in-process instead of over RabbitMQ.

## No Kaido, no RabbitMQ — and why that's not a simplification, it's the point

Every other variant needs a message bus because a command sent from one service has to physically leave the process to reach another. Here there's only one process. `RegisterUserHandler` (Identity) calling `mediator.Send(new Voyager.Contracts.Driver.AddDriver { DriverId = user.Id })` resolves directly against `AddDriverHandler` (Driver) through the **same shared `IHikyaku`** registered once in `Host/Program.cs` — no serialization, no queue, no network round-trip. `RideEventHandlers` in Hub subscribe to the same `INotification` types Ride publishes; Hikyaku's own in-process fan-out is what used to require Kaido's RabbitMQ-backed notification relay in every other variant. Removing the message bus isn't a shortcut — it's the accurate consequence of the deployment model actually being one process. If this system ever needed to become physically distributed again, extracting a module means promoting its `Voyager.Contracts` calls to Kaido calls — see [Clean.Architecture](../Clean.Architecture/README.md) or [Hexagonal.Architecture](../Hexagonal.Architecture/README.md) for what that looks like on the other side of that extraction.

## The public/internal split follows the HTTP boundary, not the folder

This is the one genuinely subtle rule in this variant, and it's driven by the compiler, not a style choice: **a public method can never expose a less-accessible type** (C# error CS0050). Since ASP.NET Core's controller discovery requires controllers to be `public`, every type that appears in a controller action's signature — request bodies bound with `[FromBody]`, `ActionResult<T>` return types — has to be `public` too, even though it lives inside a module whose whole premise is "everything is internal." Concretely, in `Driver.Module`:

| Type | Visibility | Why |
|---|---|---|
| `Entities.Driver` | `internal` | Never appears in a signature — always mapped to a response DTO by hand |
| `Entities.DriverStatus` | **`public`** | Used by `DriverStatusResponse.Status` and `UpdateAvailabilityRequest.Status`, both public DTOs |
| `Features.GetDriverStatus.GetDriverStatus` (the query) | `internal` | Constructed inside the controller body, never bound from the request |
| `Features.GetDriverStatus.DriverStatusResponse` | **`public`** | Returned by a public controller action |
| `Features.AddDriver.AddDriverHandler` | `internal` | Implementation detail, resolved by Hikyaku via DI, never referenced by name outside this assembly |
| `Features.AddDriver.AddDriverController` | `public` | ASP.NET Core's `ControllerFeatureProvider` only discovers public controllers |

The rule of thumb used throughout: **default everything to `internal`; flip a type to `public` only when the compiler says CS0050 forces it** (a `[FromBody]` parameter type or an `ActionResult<T>`/return type). Command/query envelope types, handlers, validators, `DbContext`s, and entities stay internal in every module.

## Identity owns the whole auth story

In the multi-process variants, Identity issues tokens (`AddServer`) and every other service independently validates them (`AddValidation` with `UseSystemNetHttp()` pointed at Identity's issuer URL over HTTP). Here, `Identity.Module`'s `AddIdentityModule` registers **both** `AddServer` and `AddValidation` in the same `AddOpenIddict()` chain, with `AddValidation` using `UseLocalServer()` — token validation happens entirely in-process against the server component, no HTTP round-trip to itself. Every other module's `[Authorize]` attribute rides on the single authentication scheme this registers once, process-wide.

## Persistence — still one database per module

Each module keeps its own `DbContext` and its own SQL Server database (`identity`, `driver`, `ride` — Hub has no persistence, same as every other variant). This is deliberate: a modular monolith that shares one database across modules has already given up the thing that makes modules extractable later. `Host/Program.cs` calls each module's `Migrate*Database()` at startup; there's no cross-database join anywhere, by construction — a module can only read another module's data by asking it (`IHikyaku.Send`), same discipline as if it actually were a separate service.

## Testing

Unit tests live in each module's own `*.Tests` project, granted access to that module's internal types via `[assembly: InternalsVisibleTo("{Module}.Tests")]` (in each module's `AssemblyInfo.cs`) — a different module's tests still can't reach in. `Hub.Module` additionally grants `InternalsVisibleTo("DynamicProxyGenAssembly2")`, which NSubstitute's underlying Castle DynamicProxy needs to mock an internal type (`VoyagerHub`) used as a generic argument in tests — a known, standard requirement whenever a test mocks an internal type or interface, not something specific to this codebase.

## Running

```bash
cd Modular.Monolith
docker-compose up -d
```

`docker-compose up -d` starts the infra *and* the host — that is the app. `Host/Host.csproj` is the
single deployable; to run it outside Docker use `dotnet run --project Host/Host.csproj`.
`Demo/Demo.csproj` is optional and is **not** the app: it's a standalone console client with no
project references that drives the running host over HTTP and SignalR as a walkthrough.

```bash
dotnet run --project Demo/Demo.csproj
```

| | URL | Swagger |
|---|---|---|
| Everything — Identity, Driver, Ride, Hub | http://localhost:5400 | `/swagger` |

One process, one port, one Swagger document listing every module's endpoints together. Infra (`sqlserver` 1437, `redis` 6383) is offset from the other three variants; there's no RabbitMQ container in this stack at all.

```bash
# Build / test
dotnet build Voyager.ModularMonolith.sln
dotnet test Voyager.ModularMonolith.sln
```
