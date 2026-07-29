using MediatR;
using Voyager.Contracts.Driver;

namespace Driver.Core.Ports.Primary;

/// <summary>
/// Primary port. Also extends MediatR's IRequestHandler&lt;AddDriver&gt; so the use case is
/// reachable two ways without duplication: injected directly by local primary adapters
/// (Driver.Api's controller calls .Handle(...) with no mediator indirection), and dispatched
/// remotely by Arbitrer when another service (Identity) drives this use case over the wire —
/// both are just different primary adapters for the same hexagon.
/// </summary>
public interface IAddDriverUseCase : IRequestHandler<AddDriver>;
