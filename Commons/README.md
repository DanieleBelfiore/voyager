# Commons

Code shared across architecture variants, kept deliberately small and **infra-agnostic**. Nothing here is domain or CQRS logic — each architecture folder has to stand on its own for the portfolio comparison to mean anything. See the [root README](../README.md) for the full rationale.

Used by `Clean.Architecture/` onward (`Plugin.Microservices.CQRS/` predates this split and stays self-contained with its own `Common/Common.Core`, which additionally has the plugin `Loader` — architecture-specific, not shared).

## Voyager.Shared

Generic ASP.NET Core / EF Core utilities with zero business meaning:
- `Cache/` — `ICacheService` + Redis/in-memory implementations (cache-aside helper)
- `RateLimiting/` — fixed-window rate limit policy configuration and wiring
- `Extensions/` — `ControllerExtensions.GetUserId()`, EF Core spatial-index migration helpers, a `TakeIfPositive` LINQ helper
- `Diagnostics/` — the problem-details exception handler, forwarded-headers wiring, and the `IHostedLifecycleService.StartedAsync` startup-migration hook plus the `/ready` health check that reports on it (see [DESIGN.md](../DESIGN.md) for why migrations run *after* the socket opens)
- `Validation/` — `ValidationBehavior<TRequest,TResponse>`, a Hikyaku pipeline behavior that runs registered FluentValidation validators before the handler. Generic plumbing with no domain knowledge: it resolves `IValidator<TRequest>` out of DI and throws — it defines no rule of its own. Used by the Vertical Slice and Modular Monolith variants only

- `Security/` — `OpenIddictClientSeed`, the idempotent registration of the public client the password grant is issued to. Without it OpenIddict rejects every token request with `invalid_client`, so no variant can authenticate at all
- `Serialization/` — `KaidoJsonDefaults`, the Newtonsoft settings Kaido uses on the wire. Without the geometry converter registered there, any cross-service payload carrying a `Point` fails on the receiving end — which takes out the whole real-time path

## Voyager.TestInfra

Harness for the variants' integration suites: the Testcontainers fixture (SQL Server, RabbitMQ, Redis), a truncating database reset, the registration/password-grant helper, the `WebApplicationFactory` wrapper, and the plumbing that lets one host's outbound HTTP reach another host's test server. Infrastructure only — no domain, CQRS or variant-specific logic, which is what makes it belong here under the same rule as the rest of `Commons/`.

Not referenced by any production project. `Plugin.Microservices.CQRS/` keeps its own harness, as it keeps its own `Common/Common.Core`.

## Voyager.Contracts

Message **shapes only** (no handlers, no logic) for cross-service RPC over the message bus (via Kaido/RabbitMQ). Every service in the portfolio uses **implicit remote dispatch**: `IHikyaku.Send(request)` runs locally if a handler is registered, otherwise Kaido routes it to whichever service does register one, using the request type's full name as the routing key.

That means the request type has to be the *same type* on both ends of the wire — not just structurally identical. Rather than have each service duplicate (or directly project-reference) another service's internal contracts, the shared message shape lives here. Example: `Identity/GetUsersRatings.cs` is defined once and referenced by both the Identity service (which handles it) and the Driver service (which sends it as part of the matching algorithm).

Only contracts actually needed for cross-service business logic belong here — not every DTO, and never a convenience passthrough that duplicates another service's own endpoint.

The same applies to events: `Ride/RideEvents.cs` holds `INotification` types Ride publishes and Hub subscribes to (`IHikyaku.Publish`, fanned out by Kaido the same way as requests) — this is how ride-lifecycle updates reach Hub's connected SignalR clients without Ride needing to know Hub's internals.

In layered variants (Clean/Hexagonal/etc.), a service's Application layer is allowed to reference `Voyager.Contracts` even though it can't reference anything else in Commons — it's pure message shape, not an infrastructure concern. The concrete transport (Kaido/RabbitMQ wiring) still only lives in Infrastructure.
