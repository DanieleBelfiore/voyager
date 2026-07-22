using NetTopologySuite.Geometries;

namespace Driver.Application.Dtos;

public class UpdateLocationRequest
{
  public Point Location { get; set; }
}
