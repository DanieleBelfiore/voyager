using NetTopologySuite.Geometries;

namespace Driver.Api.Features.UpdateLocation;

public class UpdateLocationRequest
{
  public Point Location { get; set; }
}
