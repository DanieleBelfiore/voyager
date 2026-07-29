using System;

namespace Ride.Core.UseCases;

/// <summary>Shared ETA math for both the local use case and the Hub-facing remote handler.</summary>
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
      >= 8 and <= 10 => 1.5,
      >= 17 and <= 19 => 1.6,
      >= 22 or <= 5 => 0.8,
      >= 12 and <= 14 => 1.3,
      _ => 1.0
    };
  }
}
