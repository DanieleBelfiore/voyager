using System;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace Hub.Api;

/// <summary>
/// Strongly-typed SignalR client contract. Lives here rather than in Hub.Application because
/// it's inherently a presentation-layer construct — the shape of what the wire protocol sends,
/// not a business abstraction.
/// </summary>
public interface IVoyagerShareClient
{
  Task SendToDriverNewRideRequest(Guid rideId);
  Task SendToDriverRideCancel(Guid rideId);
  Task SendToDriverNewRateReceived(int rate);

  Task SendToRiderNewDriverLocation(Point location);
  Task SendToRiderDriverArrival(Guid rideId);
  Task SendToRiderNewETA(int? estimatedArrivalMinutes, double? distanceKm);
  Task SendToRiderNewRateReceived(int rate);
  Task SendToRiderRideAccepted(Guid rideId);
  Task SendToRiderRideCompleted(Guid rideId);
}
