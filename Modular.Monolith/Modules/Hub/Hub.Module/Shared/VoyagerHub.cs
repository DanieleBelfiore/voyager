using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NetTopologySuite.Geometries;
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
