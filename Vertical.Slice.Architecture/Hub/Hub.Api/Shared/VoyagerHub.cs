using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NetTopologySuite.Geometries;
using UpdateDriverLocationCommand = Hub.Api.Features.UpdateDriverLocation.UpdateDriverLocation;

namespace Hub.Api.Shared;

/// <summary>
/// Primary adapter over SignalR. Sends through IMediator, same as every controller in this
/// variant — no direct handler injection. Also the target of IHubContext&lt;VoyagerHub,
/// IVoyagerShareClient&gt; used by RideEvents' notification handlers, which is why it lives in
/// Shared rather than inside Features/UpdateDriverLocation: it's shared infrastructure, not
/// exclusive to one feature.
/// </summary>
[Authorize]
public class VoyagerHub(IMediator mediator) : Hub<IVoyagerShareClient>
{
  public async Task JoinRideGroup(string rideId)
  {
    await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.ForRide(Guid.Parse(rideId)));
  }

  public async Task LeaveRideGroup(string rideId)
  {
    await Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.ForRide(Guid.Parse(rideId)));
  }

  public async Task UpdateDriverLocation(Point location)
  {
    var id = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new HubException("user_not_authenticated");

    await mediator.Send(new UpdateDriverLocationCommand { DriverId = Guid.Parse(id), Location = location }, CancellationToken.None);
  }
}
