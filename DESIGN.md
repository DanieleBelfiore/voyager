# Project Voyager — Design Document

## Approach

Rather than submitting a single implementation, this repository is a portfolio: the same
ride-sharing domain (Identity, Driver, Ride, Hub) is implemented five times, once per
architectural style — plugin-composed microservices with CQRS, Clean/Onion Architecture,
Hexagonal (Ports & Adapters), Vertical Slice Architecture, and a Modular Monolith. Each variant
is a complete, independently runnable solution with its own tests and `docker-compose.yml`. The
goal is to demonstrate the trade-offs between architectural styles on a non-trivial domain,
rather than argue for one style in the abstract.

This document describes the design decisions common to all five variants — the domain model,
the matching algorithm, the real-time approach, and the technical trade-offs — and calls out
where a specific variant diverges. **Plugin.Microservices.CQRS** is the *original* implementation
— the one the other four were derived from, and the first to be exercised against a live Docker
stack via Testcontainers integration tests. It is not the cleanest: it keeps an anemic
domain model (see "Ride requests & driver availability") and its services reach each other's
contract-only `.Core` projects directly rather than through a shared `Commons/Voyager.Contracts`.
Read it as the baseline the later variants improve on, not as the exemplar. See each variant's
own `README.md` for its specific rationale.

## Architecture

Four services, one per bounded context:

| Service | Responsibility |
|---|---|
| **Identity** | Registration, OAuth2/password-grant auth (OpenIddict), driver/rider role claim |
| **Driver** | Driver registration, live location, availability state, nearest-driver search |
| **Ride** | Ride lifecycle: request → accept → start → complete → rate |
| **Hub** | SignalR endpoint; relays ride/location/rating events to connected clients |

The mediator is **Hikyaku**, a fork of MediatR 12.5.0. The portfolio originally ran on MediatR +
**Arbitrer**; Arbitrer was archived by its author in July 2025 and its successor is Hikyaku (plus
**Kaido**, the out-of-process half) from the same author. The fork keeps MediatR's type names
verbatim — `IRequest<T>`, `IRequestHandler<,>`, `INotification`, `IPipelineBehavior<,>` — and
renames exactly one, `IMediator` → `IHikyaku`, so the migration touched wiring and `using`
directives, not a single handler body.

Services communicate over two channels: synchronous request/response and fire-and-forget
notifications, both carried by **Kaido**, which gives Hikyaku implicit remote
dispatch over RabbitMQ. `IHikyaku.Send(request)` runs locally if a handler is registered in the
current process; otherwise Kaido routes it to whichever service does, keyed by the request
type's name. The same mechanism carries `IHikyaku.Publish(notification)` fan-out for events —
e.g. Ride publishes `RideAccepted`, `RideCompleted`, etc., and Hub is the subscriber that relays
them over SignalR. This keeps services decoupled at compile time: nobody holds a project
reference to another service's internals, only to `Commons/Voyager.Contracts` (wire-shape-only
message contracts). The Modular Monolith variant reuses the identical contracts and `IHikyaku`
calls, but resolves them directly out of one shared DI container — no bus, single process —
which is the whole point of that variant: same domain code, different deployment topology.

See `Schema.png` (repo root) for the component diagram: client → REST for
search/request/accept/rate, client ⇄ WebSocket (SignalR) for live location/ETA/status, and the
four services wired through Kaido/RabbitMQ for cross-service calls and event fan-out.

## Ride requests & driver availability

A driver's live state (`Available` / `OnRide` / `Offline`) and last known location (SQL Server
`geography`, via NetTopologySuite) live in the Driver service, updated either over REST or over
the SignalR connection itself (`VoyagerHub.UpdateDriverLocation`) so the same real-time channel
carries traffic in both directions, not just server→client pushes. A ride's lifecycle is a small
state machine (`Requested → DriverAssigned → InProgress → Completed`, or `Cancelled` from either
of the first two states). **Where it is enforced is the one place the variants genuinely split.**
Clean, Hexagonal, Vertical Slice and Modular Monolith put it in the `Ride` entity itself — the
transition methods (`ride.Accept(driverId)`, `ride.Start()`, `ride.Complete(...)`) own the guard
clauses, so no caller can reach an illegal state. Plugin.Microservices.CQRS predates that: its
`Ride` is an anemic EF model (properties only) and each handler checks the current status and
assigns the next one itself. Same rules, same transitions, enforced one layer further out —
which is exactly the kind of difference the portfolio exists to make visible, but it does mean
the original variant is the weaker one on this axis, not the reference. Accepting a ride checks
that the caller is the driver the rider actually selected at request time — the endpoint is
open to any authenticated driver, but only the assigned one can transition it. Accept moves the
driver to `OnRide`; Complete/Cancel move them back to `Available`, so a driver mid-ride stops
appearing in subsequent searches.

## Matching algorithm

`SearchBestDriver` filters candidates directly in SQL with `WHERE LastLocation.STDistance(@point)
<= @radius` — real geodetic distance computed server-side (not a naive planar distance on raw
lat/lon coordinates, which is wrong by construction), backed by a spatial index so the filter
doesn't force a full table scan. Every driver that passes is then ranked with a weighted score:

```
score = distanceWeight × normalizedDistance + ratingWeight × (1 − normalizedRating)
```

Weights are configuration, not hardcoded, so the distance/rating trade-off is tunable per
deployment without a redeploy. A driver with no ratings yet is scored at the midpoint of the
rating range rather than the floor, so a brand-new driver isn't unfairly buried under drivers
with a single low rating. Results are intentionally **not** cached — driver location/availability
changes every few seconds, so a TTL-cached candidate list is either stale or constantly
invalidated; the spatially-indexed query is both cheaper and more correct.

## Fare calculation

`CompleteRide` never accepts a client-supplied price — a rider or driver could otherwise under-
or over-charge the other side. Instead the handler computes it server-side from the same
great-circle distance helper used for ETA (pickup → dropoff, not driver → pickup) and the
elapsed time between `StartRide` and `CompleteRide`:

```
price = baseFare + perKmRate × distanceKm + perMinuteRate × durationMinutes
```

`baseFare`/`perKmRate`/`perMinuteRate` are configuration, same pattern as the ETA time-of-day
multipliers — tunable per deployment without a redeploy. Surge pricing and payments are still
out of scope.

## Ride tracking

Once a ride is accepted, the driver's connection joins a `ride_{id}` SignalR group; every
subsequent location update pushes the new position, a recomputed ETA (great-circle distance,
average speed, and a configurable time-of-day multiplier for peak/night/lunch traffic), and — if
within a configurable radius of the pickup point — an arrival notification, all to that group.

The one-request-per-driver-status-change model (`NewRideRequested` targets the driver's own
`user_{driverId}` group, joined on connection, rather than `ride_{id}`) matters specifically
because at the moment a ride is requested nobody has an active ride yet, so there is no
`ride_{id}` group to join — routing to the driver's personal identity is what makes that first,
critical notification actually deliverable.

## Real-time communication

SignalR (WebSockets, with SSE/long-polling fallback) is the transport for everything client-facing
in real time. The main design challenge in a system with a message bus in the middle is that a
payload can cross two different serialization boundaries — the SignalR wire protocol and
Kaido's RabbitMQ payload — and both need to agree on how to represent domain types like
geospatial `Point`s consistently, or a message that looks fine in isolation fails silently (or
loudly) when it actually needs to travel between processes. This is why cross-service messages
are defined as flat, explicit contract types in `Commons/Voyager.Contracts` rather than reusing
rich domain/EF entities across a service boundary.

## Scalability & performance

- Independent horizontal scaling per service (four processes, four failure domains).
- Hub's SignalR hub is backed by the Redis backplane (`AddStackExchangeRedis`), so group
  membership and `Clients.Group(...)` dispatch work correctly across multiple Hub instances —
  without it, a client connected to instance A never receives a message published from instance
  B, since both are in-memory and per-instance by default.
- Redis cache-aside for read-heavy, low-churn queries (driver status lookups).
- Per-endpoint-class rate limiting (registration, location updates, search, etc.).
- SQL Server `geography` columns for driver/ride locations, queried with `STDistance` (via
  NetTopologySuite's `Point.Distance()`, translated by the EF Core provider) and backed by a
  `GEOGRAPHY_AUTO_GRID` spatial index on `Drivers.LastLocation`, so the exact-distance filter is
  both correct and index-eligible in one query — no separate bounding-box prefilter needed.
- SlowQueryInterceptor flags slow EF Core queries in logs for follow-up.
- Every service exposes `/health` (liveness — process is up, no dependency checks) and `/ready`
  (readiness — own database reachable and migrated; Hub additionally checks the Redis backplane it
  now depends on). `docker-compose.yml` uses `/ready` for each service's `HEALTHCHECK`, and
  downstream services gate on it via `depends_on: condition: service_healthy` instead of a plain
  start-order `depends_on`, so a service never accepts traffic — or gets depended on — before its
  own dependencies are actually usable.
- EF migrations run from an `IHostedLifecycleService.StartedAsync` hook rather than inline before
  `app.Run()`, so the socket is already open while they execute and `/health` stays answerable for
  the whole migration; `/ready` reports unhealthy until they finish. `StartedAsync` specifically,
  not `StartAsync`: `WebApplicationBuilder` registers the Kestrel-hosting service during `Build()`,
  *after* anything the application registered, so a plain `IHostedService` would still run first
  and block the port.

## Testing

Unit tests cover handler/use-case logic against the EF in-memory provider, and carry an 80%
line-coverage gate on the business-logic layers of every variant. They deliberately stop at the
process boundary. Handlers reaching for a relational-only EF feature (`ExecuteUpdate`,
`ExecuteDelete`, raw SQL) have no in-memory implementation and are therefore left to the
integration suites — no SQLite provider is used anywhere to paper over that gap.

Integration tests exist for the things that only fail once real infrastructure is in the loop,
and are kept to one per concern rather than mirroring the unit suite:

| Concern | Why a unit test cannot reach it |
|---|---|
| Auth gate on the HTTP pipeline | `[Authorize]`, scheme selection and token validation are middleware, not handler code |
| Nearest-driver search | `STDistance` in real geodetic metres, and the spatial index that makes it usable — EF Core's InMemory provider emulates neither |
| Redis cache round-trip | serializer behaviour on an NTS `Point`; the failure mode is a swallowed write, so the handler still "passes" |
| Cross-service dispatch | Kaido routes by request type name over RabbitMQ — the same type has to exist on both ends of the wire |
| Ride event → SignalR client | publish/fanout plus hub group membership, none of which exists in-process |
| Ride lifecycle over HTTP | state transitions persisted across separate requests |

Two caveats on what the harness proves. It substitutes the OpenIddict registration — the real
one validates against a remote issuer, which a local test server cannot be — and injects a
test-only `connect/token` endpoint, so the *shipped* auth wiring is covered by deployment, not by
these tests. And the per-test reset truncates tables rather than recreating the schema, precisely
so that migration-only artefacts such as the geospatial index stay in place; a reset that rebuilds
from the EF model tests a schema no deployment ever produces.

## Known limitations

Matching is rider-pull (search → pick → request), with no dispatch loop, offer/accept timeout,
or automatic fallback to the next-best driver if a request goes unanswered — a deliberate scope
cut for a ~10-hour exercise, not an oversight. Payments and surge pricing are out of scope. See
each variant's own README for a "Current Limitations" section specific to that implementation.
