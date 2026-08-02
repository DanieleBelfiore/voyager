using System.Threading;
using System.Threading.Tasks;
using Ride.Application.Ports;
using MediatR;
using ActiveRideInfo = Voyager.Contracts.Ride.ActiveRideInfo;
using SharedGetActiveRide = Voyager.Contracts.Ride.GetActiveRide;

namespace Ride.Application.CQRS.Queries;

/// <summary>
/// Handles the shared Voyager.Contracts.Ride.GetActiveRide contract directly — this is the
/// remote target Hub's VoyagerHub.UpdateDriverLocation dispatches to via Arbitrer, to know
/// which ride group to push a driver's new location to.
/// </summary>
public class GetActiveRideForHubHandler(IRideRepository repository) : IRequestHandler<SharedGetActiveRide, ActiveRideInfo>
{
  public async Task<ActiveRideInfo> Handle(SharedGetActiveRide request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetActiveRideAsync(request.DriverId, request.UserId, cancellationToken);

    return ride == null ? null : new ActiveRideInfo { Id = ride.Id, PickupLocation = ride.PickupLocation, HasStarted = ride.HasStarted() };
  }
}
