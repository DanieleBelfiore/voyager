using NetTopologySuite.Geometries;

namespace Driver.Application.Dtos;

public class SearchBestDriverRequest
{
  public Point Location { get; set; }
  public int DistanceThresholdInKm { get; set; }
}
