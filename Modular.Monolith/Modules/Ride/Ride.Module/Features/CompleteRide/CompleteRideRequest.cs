using NetTopologySuite.Geometries;

namespace Ride.Module.Features.CompleteRide;

public class CompleteRideRequest
{
  public Point Location { get; set; }
}
