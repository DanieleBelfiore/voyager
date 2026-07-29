using NetTopologySuite.Geometries;

namespace Driver.Module.Features.SearchBestDriver;

/// <summary>Public — bound from the request body on a public controller action, so it can't be internal (CS0050).</summary>
public class SearchBestDriverRequest
{
  public Point Location { get; set; }
  public int DistanceThresholdInKm { get; set; }
}
