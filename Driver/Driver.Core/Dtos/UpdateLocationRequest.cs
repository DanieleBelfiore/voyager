using NetTopologySuite.Geometries;

namespace Driver.Core.Dtos
{
  public class UpdateLocationRequest
  {
    public Point Location { get; set; }
  }
}
