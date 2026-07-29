using System;

namespace Ride.Application.CQRS.Queries;

/// <summary>
/// Shared ETA math for both the local query (Ride's own controller) and the remote query Hub
/// dispatches to — same weighted time-of-day multiplier as the Plugin.Microservices.CQRS variant.
/// </summary>
public static class RideEtaCalculator
{
  public static (int EstimatedArrivalMinutes, double DistanceKm) Calculate(double distanceInMeters, double averageSpeedKmh)
  {
    var baseMinutes = distanceInMeters / 1000 / averageSpeedKmh * 60;
    var adjustedMinutes = baseMinutes * GetTimeMultiplier(DateTime.Now.Hour);

    return (DateTime.UtcNow.AddMinutes(adjustedMinutes).Minute, Math.Round(distanceInMeters, 2));
  }

  private static double GetTimeMultiplier(int hour)
  {
    return hour switch
    {
      // Morning highlights (8-10)
      >= 8 and <= 10 => 1.5,

      // Evening highlights (17-19)
      >= 17 and <= 19 => 1.6,

      // Night highlights (22-5)
      >= 22 or <= 5 => 0.8,

      // Lunch time (12-14)
      >= 12 and <= 14 => 1.3,

      // Normal daytime (6-11)
      _ => 1.0
    };
  }
}
