using NetTopologySuite.Geometries;

namespace Ride.Module.Features.StartRide;

public class StartRideRequest
{
  public Point Location { get; set; }
}
