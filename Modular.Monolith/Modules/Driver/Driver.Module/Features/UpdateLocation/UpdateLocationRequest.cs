using NetTopologySuite.Geometries;

namespace Driver.Module.Features.UpdateLocation;

/// <summary>Public — bound from the request body on a public controller action, so it can't be internal (CS0050).</summary>
public class UpdateLocationRequest
{
  public Point Location { get; set; }
}
