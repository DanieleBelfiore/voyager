using NetTopologySuite.Geometries;

namespace Ride.Core.Dtos
{
  public class StartRideRequest
  {
    public Point Location { get; set; }
  }
}
