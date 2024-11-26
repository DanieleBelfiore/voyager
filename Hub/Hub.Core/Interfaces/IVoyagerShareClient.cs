using System;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using Ride.Core.Dtos;

namespace Hub.Core.Interfaces
{
  public interface IVoyagerShareClient
  {
    Task SendToDriverNewRideRequest(Guid rideId);
    Task SendToDriverRideCancel(Guid rideId);
    Task SendToDriverNewRateReceived(int rate);

    Task SendToRiderNewDriverLocation(Point location);
    Task SendToRiderDriverArrival(Guid rideId);
    Task SendToRiderNewETA(ETAResponse ETA);
    Task SendToRiderNewRateReceived(int rate);
    Task SendToRiderRideAccepted(Guid rideId);
    Task SendToRiderRideCompleted(Guid rideId);
  }
}
