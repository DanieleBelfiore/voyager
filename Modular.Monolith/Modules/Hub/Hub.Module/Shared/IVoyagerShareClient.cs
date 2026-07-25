using System;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace Hub.Module.Shared;

internal interface IVoyagerShareClient
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
