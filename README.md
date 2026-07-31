# Voyager — Architecture Portfolio

[![CI](https://github.com/DanieleBelfiore/voyager/actions/workflows/ci.yml/badge.svg)](https://github.com/DanieleBelfiore/voyager/actions/workflows/ci.yml)

Voyager is a ride-sharing backend (Identity / Driver / Ride / Hub services). This repo implements the **same domain and feature set multiple times, once per architectural style**, so each approach can be inspected, run and compared in isolation. The goal is demonstrative: showing how the same business problem is structured differently under different architectural constraints.

### System Architecture Diagram

![Schema](./Schema.png)

## Variants

| Folder | Style | Key idea | Status |
|---|---|---|---|
| [`Plugin.Microservices.CQRS/`](Plugin.Microservices.CQRS/README.md) | Plugin-composed microservices + CQRS | Services self-register via a dynamic `IModule` loader (`AssemblyLoadContext`); CQRS/MediatR inside each service; no cross-service compile-time references | ✅ done |
| [`Clean.Architecture/`](Clean.Architecture/README.md) | Clean / Onion Architecture | `Domain → Application → Infrastructure → Api`, dependencies point inward only, domain has zero framework references, ports/adapters for cache/persistence/cross-service calls | ✅ done |
| [`Hexagonal.Architecture/`](Hexagonal.Architecture/README.md) | Hexagonal (Ports & Adapters) | One core per service (entities + primary/secondary ports + use cases); every use case is reachable both by direct local injection and by remote Arbitrer dispatch through the same primary-port interface, no `IMediator.Send` in controllers | ✅ done |
| [`Vertical.Slice.Architecture/`](Vertical.Slice.Architecture/README.md) | Vertical Slice Architecture | No horizontal layers, no ports — each feature is a self-contained folder with its own request, handler, validator and controller; handlers touch EF Core/cache/IMediator directly | ✅ done |
| [`Modular.Monolith/`](Modular.Monolith/README.md) | Modular Monolith | Same four bounded contexts, deployed as a single process on one port; module boundaries enforced by C# `internal` visibility (CS0050), no message bus — `IMediator` dispatches in-process across module assemblies | ✅ done |

Each folder is a **self-contained .NET solution** with its own `README.md` (architecture rationale, patterns used) and `CLAUDE.md` (commands, layout). Don't assume commands or paths from one variant apply to another — `cd` into the folder first.

For browsing every variant side by side in a single IDE window, open [`Voyager.All.sln`](Voyager.All.sln) at the repo root — an aggregate solution containing all 81 projects, grouped into solution folders that mirror the on-disk layout. It's an IDE convenience only: each variant's own `.sln` stays the canonical build unit, and CI builds those five individually.

## `Commons/`

Reserved for code that is genuinely infra-agnostic across *all* variants — e.g. shared Docker/test-container infrastructure, cross-cutting contracts. It intentionally holds **no domain or CQRS logic**: each architecture folder has to stand on its own, or the comparison is meaningless.

## Services (same across every variant)

| Service | Port | Responsibility |
|---------|------|----------------|
| Identity | 5001 | Auth, user registration, OAuth2/OpenID (OpenIddict) |
| Driver | 5002 | Driver registration, location updates, availability, matching |
| Ride | 5003 | Ride lifecycle: request → accept → start → complete → rate |
| Hub | 5000 | Real-time SignalR hub: broadcasts location/status events to riders |

## Infrastructure

- **SQL Server**: persistent storage with spatial (geography) support
- **Redis**: distributed caching
- **RabbitMQ**: inter-service messaging
- **SignalR**: real-time push to clients
- **Docker Compose**: per-variant infra bring-up (each folder has its own `docker-compose.yml`)

## Cross-service communication

Every multi-process variant uses the same mechanism: **Arbitrer** gives MediatR implicit remote dispatch over RabbitMQ. `IMediator.Send(request)` executes locally if a handler is registered in that service; otherwise Arbitrer routes it to whichever service does, keyed by the request type's full name. This is how, for example, Driver's matching algorithm asks Identity for user ratings without a direct HTTP call or a shared database. The same mechanism also carries `IMediator.Publish(notification)` fan-out for events — e.g. Ride publishes ride-lifecycle events that Hub subscribes to and relays over SignalR (see [Clean.Architecture/README.md](Clean.Architecture/README.md#cross-service-communication) for a case where this replaced a non-functional direct cross-process call in the original implementation). The wire contracts for these calls live in `Commons/Voyager.Contracts` — see [Commons/README.md](Commons/README.md) for why that's a Commons concern and not domain logic. **Modular.Monolith is the one exception**: it reuses the exact same `Voyager.Contracts` request/notification types and the same `IMediator.Send`/`.Publish` calls, but with no Arbitrer and no RabbitMQ — everything runs in one process, so the mediator resolves the target handler directly out of one shared DI container instead of routing it over a message bus.

## Running a variant

```bash
cd Plugin.Microservices.CQRS   # or whichever variant you want
docker-compose up -d
dotnet run --project Demo/Demo.csproj
```

See the variant's own README for details, limitations, and test instructions.
