using System;
using NetTopologySuite.Geometries;

namespace Ride.Core.Dtos
{
  public class RideRequest
  {
    public Guid UserId { get; set; }
    public Guid DriverId { get; set; }
    public Point PickupLocation { get; set; }
    public Point DropoffLocation { get; set; }
  }
}
