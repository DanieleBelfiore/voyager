using System;
using NetTopologySuite.Geometries;

namespace Ride.Core.Dtos
{
  public class RideCurrentLocationResponse
  {
    public Point LastLocation { get; set; }
    public DateTime LastUpdateDate { get; set; }
  }
}
