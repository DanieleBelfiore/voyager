using System;
using Driver.Core.Enums;
using NetTopologySuite.Geometries;

namespace Driver.Core.Dtos
{
  public class DriverStatusResponse
  {
    public Guid Id { get; set; }
    public string LicenseNumber { get; set; }
    public string VehicleInfo { get; set; }
    public DriverStatus Status { get; set; }
    public Point LastLocation { get; set; }
    public DateTime LastUpdateDate { get; set; }
  }
}
