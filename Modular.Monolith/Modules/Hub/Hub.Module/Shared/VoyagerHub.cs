using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NetTopologySuite.Geometries;
using Voyager.Contracts.Ride;
using UpdateDriverLocationCommand = Hub.Module.Features.UpdateDriverLocation.UpdateDriverLocation;

namespace Hub.Module.Shared;

/// <summary>
/// Internal — only HubModuleExtensions.MapHubModule (same assembly) ever names this type
/// directly; Host just calls that extension method and never touches VoyagerHub itself.
/// </summary>
[Authorize]
internal class VoyagerHub(IMediator mediator) : Hub<IVoyagerShareClient>
{
  public async Task JoinRideGroup(string rideId)
  {
    var callerId = GetCallerId();
    var ride = await mediator.Send(new GetActiveRide { DriverId = callerId, UserId = callerId }, CancellationToken.None);
    if (ride == null || ride.Id != Guid.Parse(rideId))
      throw new HubException("not_ride_participant");

    await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.ForRide(Guid.Parse(rideId)));
  }

  public async Task LeaveRideGroup(string rideId)
  {
    await Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.ForRide(Guid.Parse(rideId)));
  }

  public async Task UpdateDriverLocation(Point location)
  {
    await mediator.Send(new UpdateDriverLocationCommand { DriverId = GetCallerId(), Location = location }, CancellationToken.None);
  }

  private Guid GetCallerId() =>
    Guid.Parse(Context.User?.FindFirst("sub")?.Value ?? throw new HubException("user_not_authenticated"));
}
