using System;
using NetTopologySuite.Geometries;

namespace Driver.Application.Dtos;

public class SearchBestDriverResponse
{
  public Guid DriverId { get; set; }
  public Point LastLocation { get; set; }
  public DateTime LastUpdateDate { get; set; }
  public double Distance { get; set; }
  public double Score { get; set; }
}
