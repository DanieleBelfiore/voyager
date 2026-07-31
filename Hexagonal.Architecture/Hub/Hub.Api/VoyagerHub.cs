using System;
using System.Threading;
using System.Threading.Tasks;
using Hub.Core.Ports.Primary;
using Hub.Core.Ports.Secondary;
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
public class VoyagerHub(IUpdateDriverLocationUseCase updateDriverLocation, IActiveRideQuery activeRideQuery) : Hub<IVoyagerShareClient>
{
  public async Task JoinRideGroup(string rideId)
  {
    var ride = await activeRideQuery.GetActiveRideForParticipantAsync(GetCallerId(), CancellationToken.None);
    if (ride == null || ride.Id != Guid.Parse(rideId))
      throw new HubException("not_ride_participant");

    await Groups.AddToGroupAsync(Context.ConnectionId, $"ride_{rideId}");
  }

  public async Task LeaveRideGroup(string rideId)
  {
    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ride_{rideId}");
  }

  public async Task UpdateDriverLocation(Point location)
  {
    await updateDriverLocation.Handle(new Hub.Core.Ports.Primary.UpdateDriverLocation { DriverId = GetCallerId(), Location = location }, CancellationToken.None);
  }

  private Guid GetCallerId() =>
    Guid.Parse(Context.User?.FindFirst("sub")?.Value ?? throw new HubException("user_not_authenticated"));
}
