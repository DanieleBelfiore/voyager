using System;
using NetTopologySuite.Geometries;

namespace Ride.Module.Features.GetRideCurrentLocation;

public class RideCurrentLocationResponse
{
  public Point LastLocation { get; set; }
  public DateTime LastUpdateDate { get; set; }
}
