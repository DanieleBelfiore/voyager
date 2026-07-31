using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Driver.Core.CQRS.Commands;
using Hub.Core.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.Geometries;
using Ride.Core.CQRS.Queries;

namespace Hub.API;

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
public class VoyagerHub(IMediator mediator, IConfiguration configuration) : Hub<IVoyagerShareClient>
{
  /// <summary>
  /// Adds the current connection to a ride group, after verifying the caller is a
  /// participant (rider or driver) of that ride.
  /// </summary>
  /// <param name="rideId">The ID of the ride.</param>
  public async Task JoinRideGroup(string rideId)
  {
    var callerId = GetCallerId();
    var ride = await mediator.Send(new GetActiveRide { DriverId = callerId, UserId = callerId });
    if (ride == null || ride.Id != Guid.Parse(rideId))
      throw new HubException("not_ride_participant");

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
    var driverId = GetCallerId();

    await mediator.Send(new UpdateLocation { Id = driverId, Location = location });

    var ride = await mediator.Send(new GetActiveRide { DriverId = driverId });
    if (ride == null)
      return;

    await Clients.Group($"ride_{ride.Id}").SendToRiderNewDriverLocation(location);

    var ETA = await mediator.Send(new GetRideETA { Id = ride.Id });

    await Clients.Group($"ride_{ride.Id}").SendToRiderNewETA(ETA);

    var distance = DistanceInMeters(ride.PickupLocation, location);
    if (distance < configuration.GetValue<double>("Hub:ArrivalThresholdMeters"))
      await Clients.Group($"ride_{ride.Id}").SendToRiderDriverArrival(ride.Id);
  }

  private Guid GetCallerId() =>
    Guid.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new HubException("user_not_authenticated"));

  // NetTopologySuite's Point.Distance() is planar/Cartesian on the raw coordinate values (SRID
  // is metadata only) — on lat/lon points that returns degrees, not meters. Haversine gives the
  // real great-circle distance so it's comparable against the configured arrival threshold.
  private static double DistanceInMeters(Point a, Point b)
  {
    const double earthRadiusMeters = 6371000;

    var lat1 = a.Y * Math.PI / 180;
    var lat2 = b.Y * Math.PI / 180;
    var deltaLat = (b.Y - a.Y) * Math.PI / 180;
    var deltaLon = (b.X - a.X) * Math.PI / 180;

    var h = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
            Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

    return 2 * earthRadiusMeters * Math.Asin(Math.Sqrt(h));
  }
}
