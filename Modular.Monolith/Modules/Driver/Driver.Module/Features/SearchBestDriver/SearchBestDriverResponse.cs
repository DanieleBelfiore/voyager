using System;
using NetTopologySuite.Geometries;

namespace Driver.Module.Features.SearchBestDriver;

/// <summary>Public — returned from a public controller action, so it can't be internal (CS0050).</summary>
public class SearchBestDriverResponse
{
  public Guid DriverId { get; set; }
  public Point LastLocation { get; set; }
  public DateTime LastUpdateDate { get; set; }
  public double Distance { get; set; }
  public double Score { get; set; }
}
