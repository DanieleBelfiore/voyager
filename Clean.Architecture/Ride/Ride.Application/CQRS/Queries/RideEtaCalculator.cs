using System;
using NetTopologySuite.Geometries;
using Ride.Application.Ports;

namespace Ride.Application.CQRS.Queries;

/// <summary>
/// Shared ETA math for both the local query (Ride's own controller) and the remote query Hub
/// dispatches to — same weighted time-of-day multiplier as the Plugin.Microservices.CQRS variant.
/// </summary>
public static class RideEtaCalculator
{
  public static (int EstimatedArrivalMinutes, double DistanceKm) Calculate(double distanceInMeters, IEtaConfig config)
  {
    var baseMinutes = distanceInMeters / 1000 / config.AverageSpeedKmh * 60;
    var adjustedMinutes = baseMinutes * GetTimeMultiplier(DateTime.UtcNow.Hour, config);

    return ((int)Math.Round(adjustedMinutes), Math.Round(distanceInMeters / 1000, 2));
  }

  private static double GetTimeMultiplier(int hour, IEtaConfig config)
  {
    return hour switch
    {
      // Morning highlights (8-10)
      >= 8 and <= 10 => config.MorningPeakMultiplier,

      // Evening highlights (17-19)
      >= 17 and <= 19 => config.EveningPeakMultiplier,

      // Night highlights (22-5)
      >= 22 or <= 5 => config.NightMultiplier,

      // Lunch time (12-14)
      >= 12 and <= 14 => config.LunchMultiplier,

      // Normal daytime (6-11)
      _ => 1.0
    };
  }

  // NetTopologySuite's Point.Distance() is planar/Cartesian on the raw coordinate values (SRID
  // is metadata only) — on lat/lon points that returns degrees, not meters. Haversine gives the
  // real great-circle distance so this and the arrival-threshold checks are in the same unit.
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
