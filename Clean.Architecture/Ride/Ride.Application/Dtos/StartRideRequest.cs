using NetTopologySuite.Geometries;

namespace Ride.Application.Dtos;

public class StartRideRequest
{
  public Point Location { get; set; }
}
