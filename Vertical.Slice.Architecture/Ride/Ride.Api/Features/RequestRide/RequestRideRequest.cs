using System;
using NetTopologySuite.Geometries;

namespace Ride.Api.Features.RequestRide;

public class RequestRideRequest
{
  public Guid DriverId { get; set; }
  public Point PickupLocation { get; set; }
  public Point DropoffLocation { get; set; }
}
