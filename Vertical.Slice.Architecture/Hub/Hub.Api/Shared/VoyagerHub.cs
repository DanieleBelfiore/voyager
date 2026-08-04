using System;
using System.Threading;
using System.Threading.Tasks;
using Hikyaku;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NetTopologySuite.Geometries;
using Voyager.Contracts.Ride;
using UpdateDriverLocationCommand = Hub.Api.Features.UpdateDriverLocation.UpdateDriverLocation;

namespace Hub.Api.Shared;

/// <summary>
/// Primary adapter over SignalR. Sends through IHikyaku, same as every controller in this
/// variant — no direct handler injection. Also the target of IHubContext&lt;VoyagerHub,
/// IVoyagerShareClient&gt; used by RideEvents' notification handlers, which is why it lives in
/// Shared rather than inside Features/UpdateDriverLocation: it's shared infrastructure, not
/// exclusive to one feature.
/// </summary>
[Authorize]
public class VoyagerHub(IHikyaku mediator) : Hub<IVoyagerShareClient>
{
  /// <summary>Joins the caller's personal group so cross-service events targeting them by user
  /// id (e.g. NewRideRequested, before anyone can join ride_{RideId}) reach this connection.</summary>
  public override async Task OnConnectedAsync()
  {
    await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.ForUser(GetCallerId()));
    await base.OnConnectedAsync();
  }

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
