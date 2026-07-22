using System;
using System.Threading;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace Hub.Core.Ports.Secondary;

public interface IHubRelay
{
  Task SendToDriverNewRideRequest(Guid rideId, CancellationToken cancellationToken);
  Task SendToDriverRideCancel(Guid rideId, CancellationToken cancellationToken);
  Task SendToDriverNewRateReceived(Guid rideId, int rating, CancellationToken cancellationToken);
  Task SendToRiderNewDriverLocation(Guid rideId, Point location, CancellationToken cancellationToken);
  Task SendToRiderDriverArrival(Guid rideId, CancellationToken cancellationToken);
  Task SendToRiderNewETA(Guid rideId, int? estimatedArrivalMinutes, double? distanceKm, CancellationToken cancellationToken);
  Task SendToRiderNewRateReceived(Guid rideId, int rating, CancellationToken cancellationToken);
  Task SendToRiderRideAccepted(Guid rideId, CancellationToken cancellationToken);
  Task SendToRiderRideCompleted(Guid rideId, CancellationToken cancellationToken);
}
