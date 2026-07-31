using Ride.Module.Shared;
using Xunit;

namespace Ride.Tests.Features;

public class RideEtaCalculatorTests
{
  private static readonly EtaConfig TestConfig = new()
  {
    AverageSpeedKmh = 30,
    MorningPeakMultiplier = 1.5,
    EveningPeakMultiplier = 1.6,
    NightMultiplier = 0.8,
    LunchMultiplier = 1.3
  };

  [Fact]
  public void Calculate_ConvertsMetersToKilometers_RoundedToTwoDecimals()
  {
    // Act
    var (_, distanceKm) = RideEtaCalculator.Calculate(1234.5678, TestConfig);

    // Assert
    Assert.Equal(1.23, distanceKm);
  }

  [Fact]
  public void Calculate_ReturnsPositiveEstimatedArrivalMinutes_NotClockMinute()
  {
    // Act
    var (minutes, _) = RideEtaCalculator.Calculate(5000, TestConfig);

    // Assert: 5km at 30km/h is 10 base minutes; the time-of-day multiplier (0.8x-1.6x)
    // keeps it well under 60, so this also guards against the old bug of returning
    // DateTime.Now.Minute (a clock reading unrelated to trip length).
    Assert.InRange(minutes, 8, 16);
  }

  [Fact]
  public void Calculate_ReturnsZeroDistance_WhenAtSameLocation()
  {
    // Act
    var (_, distanceKm) = RideEtaCalculator.Calculate(0, TestConfig);

    // Assert
    Assert.Equal(0, distanceKm);
  }
}
