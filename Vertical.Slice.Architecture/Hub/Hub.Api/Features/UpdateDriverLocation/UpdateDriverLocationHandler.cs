using System.Threading;
using System.Threading.Tasks;
using Hub.Api.Shared;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Voyager.Contracts.Driver;
using Voyager.Contracts.Ride;

namespace Hub.Api.Features.UpdateDriverLocation;

/// <summary>
/// Update the driver's location, find their active ride, and push a location + ETA update to
/// the rider — plus an arrival notification once within 500m of pickup. No port for any of
/// this — IMediator.Send reaches Driver/Ride remotely via Arbitrer, IHubContext pushes over
/// SignalR directly.
/// </summary>
public class UpdateDriverLocationHandler(IMediator mediator, IHubContext<VoyagerHub, IVoyagerShareClient> hub) : IRequestHandler<UpdateDriverLocation>
{
  private const double ArrivalThresholdMeters = 500;

  public async Task Handle(UpdateDriverLocation request, CancellationToken cancellationToken)
  {
    await mediator.Send(new Voyager.Contracts.Driver.UpdateLocation { Id = request.DriverId, Location = request.Location }, cancellationToken);

    var ride = await mediator.Send(new GetActiveRide { DriverId = request.DriverId }, cancellationToken);
    if (ride == null)
      return;

    var group = HubGroups.ForRide(ride.Id);

    await hub.Clients.Group(group).SendToRiderNewDriverLocation(request.Location);

    var eta = await mediator.Send(new GetRideETA { Id = ride.Id }, cancellationToken);

    await hub.Clients.Group(group).SendToRiderNewETA(eta.EstimatedArrivalMinutes, eta.DistanceKm);

    var distance = ride.PickupLocation.Distance(request.Location);
    if (distance < ArrivalThresholdMeters)
      await hub.Clients.Group(group).SendToRiderDriverArrival(ride.Id);
  }
}
