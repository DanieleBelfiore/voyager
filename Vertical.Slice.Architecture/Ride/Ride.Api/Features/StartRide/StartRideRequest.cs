using NetTopologySuite.Geometries;

namespace Ride.Api.Features.StartRide;

public class StartRideRequest
{
  public Point Location { get; set; }
}
