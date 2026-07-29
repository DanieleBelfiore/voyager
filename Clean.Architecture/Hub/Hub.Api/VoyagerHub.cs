using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Hub.Application.CQRS.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NetTopologySuite.Geometries;

namespace Hub.Api;

/// <summary>
/// Thin SignalR entry point — unlike the Plugin.Microservices.CQRS variant, the actual
/// orchestration (update location, find active ride, push updates) lives in Application's
/// UpdateDriverLocationHandler, not here. This class only translates the wire call into a
/// mediator dispatch, the same role a controller action plays for HTTP.
/// </summary>
[Authorize]
public class VoyagerHub(IMediator mediator) : Hub<IVoyagerShareClient>
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

    await mediator.Send(new UpdateDriverLocation { DriverId = Guid.Parse(id), Location = location });
  }
}
