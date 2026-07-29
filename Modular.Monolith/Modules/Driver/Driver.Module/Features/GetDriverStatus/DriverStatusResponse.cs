using System;
using Driver.Module.Entities;
using NetTopologySuite.Geometries;

namespace Driver.Module.Features.GetDriverStatus;

/// <summary>Public — returned from a public controller action, so it can't be internal (CS0050).</summary>
public class DriverStatusResponse
{
  public Guid Id { get; set; }
  public DriverStatus Status { get; set; }
  public Point LastLocation { get; set; }
  public DateTime LastUpdateDate { get; set; }
}
