using NetTopologySuite.Geometries;

namespace Ride.Application.Dtos;

public class CompleteRideRequest
{
  public Point Location { get; set; }
}
