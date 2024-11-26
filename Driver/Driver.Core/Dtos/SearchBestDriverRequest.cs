using NetTopologySuite.Geometries;

namespace Driver.Core.Dtos
{
  public class SearchBestDriverRequest
  {
    public Point Location { get; set; }
    public int DistanceThresholdInKm { get; set; }
  }
}
