using System.Threading;
using System.Threading.Tasks;
using Hub.Module.Shared;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Voyager.Contracts.Driver;
using Voyager.Contracts.Ride;

namespace Hub.Module.Features.UpdateDriverLocation;

internal class UpdateDriverLocationHandler(IMediator mediator, IHubContext<VoyagerHub, IVoyagerShareClient> hub) : IRequestHandler<UpdateDriverLocation>
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
