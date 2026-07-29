using System;
using NetTopologySuite.Geometries;

namespace Ride.Application.Dtos;

public class RideRequest
{
  public Guid DriverId { get; set; }
  public Point PickupLocation { get; set; }
  public Point DropoffLocation { get; set; }
}
