using NetTopologySuite.Geometries;

namespace Driver.Api.Features.SearchBestDriver;

public class SearchBestDriverRequest
{
  public Point Location { get; set; }
  public int DistanceThresholdInKm { get; set; }
}
