using System;
using NetTopologySuite.Geometries;

namespace Ride.Application.Dtos;

public class RideCurrentLocationResponse
{
  public Point LastLocation { get; set; }
  public DateTime LastUpdateDate { get; set; }
}
