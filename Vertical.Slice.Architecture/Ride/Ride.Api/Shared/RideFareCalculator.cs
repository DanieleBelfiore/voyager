using System;

namespace Ride.Api.Shared;

/// <summary>
/// Fare is always computed here from distance/duration, never trusted from the client —
/// a rider/driver-supplied price would let either side under- or over-charge the other.
/// </summary>
public static class RideFareCalculator
{
  public static double Calculate(double distanceInMeters, double durationMinutes, FareConfig config)
  {
    var price = config.BaseFare
      + config.PerKmRate * (distanceInMeters / 1000)
      + config.PerMinuteRate * durationMinutes;

    return Math.Round(price, 2);
  }
}
