using System;
using NetTopologySuite.Geometries;

namespace Ride.Api.Features.GetRideCurrentLocation;

public class RideCurrentLocationResponse
{
  public Point LastLocation { get; set; }
  public DateTime LastUpdateDate { get; set; }
}
