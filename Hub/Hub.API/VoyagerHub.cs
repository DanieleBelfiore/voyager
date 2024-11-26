using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Driver.Core.CQRS.Commands;
using Hub.Core.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NetTopologySuite.Geometries;
using Ride.Core.CQRS.Queries;

namespace Hub.API
{
  /// <summary>
  /// SignalR hub for managing driver and rider communication.
  /// Real-time communication hub for ride-sharing operations.
  /// Handles WebSocket connections for live updates between drivers and riders.
  /// Uses SignalR for:
  /// - Real-time location updates from drivers
  /// - Instant notifications for ride status changes
  /// - Direct communication between riders and drivers
  /// </summary>
  [Authorize]
  public class VoyagerHub(IMediator mediator) : Hub<IVoyagerShareClient>
  {
    /// <summary>
    /// Adds the current connection to a ride group.
    /// </summary>
    /// <param name="rideId">The ID of the ride.</param>
    public async Task JoinRideGroup(string rideId)
    {
      await Groups.AddToGroupAsync(Context.ConnectionId, $"ride_{rideId}");
    }

    /// <summary>
    /// Removes the current connection from a ride group.
    /// </summary>
    /// <param name="rideId">The ID of the ride.</param>
    public async Task LeaveRideGroup(string rideId)
    {
      await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ride_{rideId}");
    }

    /// <summary>
    /// Updates the driver's location and notifies the riders in the ride group.
    /// </summary>
    /// <param name="location">The new location of the driver.</param>
    public async Task UpdateDriverLocation(Point location)
    {
      var id = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new HubException("user_not_authenticated");

      var driverId = Guid.Parse(id);

      await mediator.Send(new UpdateLocation { Id = driverId, Location = location });

      var ride = await mediator.Send(new GetActiveRide { DriverId = driverId });
      if (ride == null)
        return;

      await Clients.Group($"ride_{ride.Id}").SendToRiderNewDriverLocation(location);

      var ETA = await mediator.Send(new GetRideETA { Id = ride.Id });

      await Clients.Group($"ride_{ride.Id}").SendToRiderNewETA(ETA);

      var distance = ride.PickupLocation.Distance(location);
      if (distance < 500)
        await Clients.Group($"ride_{ride.Id}").SendToRiderDriverArrival(ride.Id);
    }
  }
}
