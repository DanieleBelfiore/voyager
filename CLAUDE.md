# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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
{Service}.Handlers/     → Handlers, EF DbContext, repositories, AutoMapper profiles, EF migrations
```

Test projects (`{Service}.Tests`, `{Service}.IntegrationTests`) sit alongside.

### Plugin / Module System

`Common.Core.Loader` dynamically discovers `IModule` implementations at startup. Every `.Handlers` project exposes a module that:
- registers its own services (`ConfigureServices`)
- runs DB migrations (`OnStartup`)
- maps routes (`UseEndpoints`)

`Program.cs` in each host calls `Loader.Current.Compose()` — this is the entry point for understanding how all DI registrations and migrations occur without explicit references between host and handler projects.

### CQRS Flow

Controllers → `IMediator.Send(command/query)` → Handler in `.Handlers` → Repository/DbContext → Response.

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

- Uses **Newtonsoft.Json** throughout (not `System.Text.Json`). Geometry types need custom converters — see existing `GeoJsonConverter` usage.
- Integration tests currently non-functional; unit tests are illustrative, not exhaustive.
- Nullable reference types are **disabled** in main projects, enabled only in test projects.
- `.editorconfig` enforces 2-space indentation and LF line endings.
