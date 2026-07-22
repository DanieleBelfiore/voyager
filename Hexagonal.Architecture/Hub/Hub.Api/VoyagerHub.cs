using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Hub.Core.Ports.Primary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NetTopologySuite.Geometries;

namespace Hub.Api;

/// <summary>
/// Primary adapter over SignalR — injects IUpdateDriverLocationUseCase directly,
/// no IMediator.Send. The same use case is still reachable remotely if ever needed, since it
/// implements IRequestHandler&lt;T&gt; too (see IUpdateDriverLocationUseCase).
/// </summary>
[Authorize]
public class VoyagerHub(IUpdateDriverLocationUseCase updateDriverLocation) : Hub<IVoyagerShareClient>
{
  public async Task JoinRideGroup(string rideId)
  {
    await Groups.AddToGroupAsync(Context.ConnectionId, $"ride_{rideId}");
  }

  public async Task LeaveRideGroup(string rideId)
  {
    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ride_{rideId}");
  }

  public async Task UpdateDriverLocation(Point location)
  {
    var id = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new HubException("user_not_authenticated");

    await updateDriverLocation.Handle(new Hub.Core.Ports.Primary.UpdateDriverLocation { DriverId = Guid.Parse(id), Location = location }, CancellationToken.None);
  }
}
