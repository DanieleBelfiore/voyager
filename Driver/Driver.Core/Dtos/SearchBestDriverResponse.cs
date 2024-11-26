using System;
using NetTopologySuite.Geometries;

namespace Driver.Core.Dtos
{
  public class SearchBestDriverResponse
  {
    public Guid DriverId { get; set; }
    public string LicenseNumber { get; set; }
    public string VehicleInfo { get; set; }
    public Point LastLocation { get; set; }
    public DateTime LastUpdateDate { get; set; }
    public double Distance { get; set; }
    public double Score { get; set; }
  }
}
