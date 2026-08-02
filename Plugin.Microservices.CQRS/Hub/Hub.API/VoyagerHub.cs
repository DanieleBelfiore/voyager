using System;
using System.Threading.Tasks;
using Driver.Core.CQRS.Commands;
using Hub.Core.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.Geometries;
using Ride.Core.CQRS.Commands;
using Ride.Core.CQRS.Queries;
using Ride.Core.Enums;

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
  /// Joins the caller's personal group so cross-service events targeting them by user id
  /// (e.g. NewRideRequested, before anyone can join ride_{RideId}) reach this connection.
  /// </summary>
  public override async Task OnConnectedAsync()
  {
    await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{GetCallerId()}");
    await base.OnConnectedAsync();
  }

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

    // Ride owns its own row, so it has to be told: the position above was applied to the Driver
    // aggregate only, leaving Ride.LastLocation — the field GET /rides/{id}/location reads —
    // frozen at the pickup point for the whole trip.
    await mediator.Send(new TrackRideLocation { RideId = ride.Id, Location = location });

    await Clients.Group($"ride_{ride.Id}").SendToRiderNewDriverLocation(location);

    // CallerId must be set: GetRideETAHandler rejects the request otherwise (defaults to
    // Guid.Empty, which matches neither the ride's rider nor driver) — the driver reporting
    // this location update is by construction a participant of their own active ride.
    var ETA = await mediator.Send(new GetRideETA { Id = ride.Id, CallerId = driverId });

    await Clients.Group($"ride_{ride.Id}").SendToRiderNewETA(ETA);

    // Only while the driver is still on their way. Once the trip starts, PickupLocation holds
    // wherever the driver was at Start, so this distance measures how far they have driven —
    // under the threshold for the first several hundred metres, which re-fired "driver has
    // arrived" over and over mid-trip.
    if (ride.Status == RideStatus.InProgress)
      return;

    var distance = DistanceInMeters(ride.PickupLocation, location);
    if (distance < configuration.GetValue<double>("Hub:ArrivalThresholdMeters"))
      await Clients.Group($"ride_{ride.Id}").SendToRiderDriverArrival(ride.Id);
  }

  // "sub", not ClaimTypes.NameIdentifier: OpenIddict issues the access token from the "sub"
  // claim (see UsersController.CreatePrincipalAsync), and every REST controller resolves the
  // caller the same way via ControllerExtensions.GetUserId(). NameIdentifier is never present,
  // so every hub method looking up the caller failed with "user_not_authenticated" before this.
  private Guid GetCallerId() =>
    Guid.Parse(Context.User?.FindFirst("sub")?.Value ?? throw new HubException("user_not_authenticated"));

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
