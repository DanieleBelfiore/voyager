using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Ride.Core.Ports.Secondary;
using ActiveRideInfo = Voyager.Contracts.Ride.ActiveRideInfo;
using SharedGetActiveRide = Voyager.Contracts.Ride.GetActiveRide;

namespace Ride.Core.UseCases;

/// <summary>
/// Handles the shared Voyager.Contracts.Ride.GetActiveRide contract directly — reachable only
/// via Arbitrer's remote dispatch (Hub), not injected by any local primary adapter, so it has
/// no dedicated primary port interface — it implements IRequestHandler&lt;T&gt; itself, which
/// is all MediatR's assembly scan needs to find it.
/// </summary>
public class GetActiveRideForHubUseCase(IRideRepository repository) : IRequestHandler<SharedGetActiveRide, ActiveRideInfo>
{
  public async Task<ActiveRideInfo> Handle(SharedGetActiveRide request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetActiveRideAsync(request.DriverId, request.UserId, cancellationToken);

    return ride == null ? null : new ActiveRideInfo { Id = ride.Id, PickupLocation = ride.PickupLocation };
  }
}
