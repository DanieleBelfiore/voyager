using System;
using Driver.Api.Entities;
using NetTopologySuite.Geometries;

namespace Driver.Api.Features.GetDriverStatus;

public class DriverStatusResponse
{
  public Guid Id { get; set; }
  public DriverStatus Status { get; set; }
  public Point LastLocation { get; set; }
  public DateTime LastUpdateDate { get; set; }
}
