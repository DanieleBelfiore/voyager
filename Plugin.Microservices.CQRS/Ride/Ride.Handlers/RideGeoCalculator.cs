using System;
using NetTopologySuite.Geometries;

namespace Ride.Handlers;

// NetTopologySuite's Point.Distance() is planar/Cartesian on the raw coordinate values (SRID
// is metadata only) — on lat/lon points that returns degrees, not meters. Haversine gives the
// real great-circle distance instead. Shared by GetRideETAHandler and CompleteRideHandler so the
// formula lives in exactly one place.
public static class RideGeoCalculator
{
  public static double DistanceInMeters(Point a, Point b)
  {
    const double earthRadiusMeters = 6371000;

    var lat1 = a.Y * Math.PI / 180;
    var lat2 = b.Y * Math.PI / 180;
    var deltaLat = (b.Y - a.Y) * Math.PI / 180;
    var deltaLon = (b.X - a.X) * Math.PI / 180;

    var h = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
            Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

    return 2 * earthRadiusMeters * Math.Asin(Math.Sqrt(h));
  }
}
