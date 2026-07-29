using System;
using Driver.Domain.Enums;
using NetTopologySuite.Geometries;

namespace Driver.Application.Dtos;

public class DriverStatusResponse
{
  public Guid Id { get; set; }
  public DriverStatus Status { get; set; }
  public Point LastLocation { get; set; }
  public DateTime LastUpdateDate { get; set; }
}
