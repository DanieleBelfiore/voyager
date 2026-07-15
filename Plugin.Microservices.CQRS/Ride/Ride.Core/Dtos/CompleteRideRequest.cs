using NetTopologySuite.Geometries;

namespace Ride.Core.Dtos
{
  public class CompleteRideRequest
  {
    public Point Location { get; set; }
    public double Price { get; set; }
  }
}
