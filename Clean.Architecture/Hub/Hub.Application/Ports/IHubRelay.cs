using System;
using System.Threading;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace Hub.Application.Ports;

/// <summary>
/// Port for pushing real-time updates to connected clients, grouped by ride. Infrastructure/Api
/// implements this over SignalR — Application only knows it needs to notify a ride's group.
/// </summary>
public interface IHubRelay
{
  Task SendToDriverNewRideRequest(Guid rideId, Guid driverId, CancellationToken cancellationToken);
  /// <summary>Takes both participant ids, not just the ride id: a cancellation can fire before
  /// anyone has joined the ride group, so this one has to be routed per-user.</summary>
  Task SendToDriverRideCancel(Guid rideId, Guid driverId, Guid userId, CancellationToken cancellationToken);
  Task SendToDriverNewRateReceived(Guid rideId, int rating, CancellationToken cancellationToken);
  Task SendToRiderNewDriverLocation(Guid rideId, Point location, CancellationToken cancellationToken);
  Task SendToRiderDriverArrival(Guid rideId, CancellationToken cancellationToken);
  Task SendToRiderNewETA(Guid rideId, int? estimatedArrivalMinutes, double? distanceKm, CancellationToken cancellationToken);
  Task SendToRiderNewRateReceived(Guid rideId, int rating, CancellationToken cancellationToken);
  Task SendToRiderRideAccepted(Guid rideId, CancellationToken cancellationToken);
  Task SendToRiderRideCompleted(Guid rideId, CancellationToken cancellationToken);
}
