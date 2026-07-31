using Ride.Core.Ports.Secondary;
using Ride.Core.UseCases;
using Xunit;

namespace Ride.Tests.UseCases;

public class RideEtaCalculatorTests
{
  private sealed class TestEtaConfig : IEtaConfig
  {
    public double AverageSpeedKmh { get; init; }
    public double MorningPeakMultiplier { get; init; } = 1.5;
    public double EveningPeakMultiplier { get; init; } = 1.6;
    public double NightMultiplier { get; init; } = 0.8;
    public double LunchMultiplier { get; init; } = 1.3;
  }

  [Fact]
  public void Calculate_ConvertsMetersToKilometers_RoundedToTwoDecimals()
  {
    // Act
    var (_, distanceKm) = RideEtaCalculator.Calculate(1234.5678, new TestEtaConfig { AverageSpeedKmh = 30 });

    // Assert
    Assert.Equal(1.23, distanceKm);
  }

  [Fact]
  public void Calculate_ReturnsPositiveEstimatedArrivalMinutes_NotClockMinute()
  {
    // Act
    var (minutes, _) = RideEtaCalculator.Calculate(5000, new TestEtaConfig { AverageSpeedKmh = 30 });

    // Assert: 5km at 30km/h is 10 base minutes; the time-of-day multiplier (0.8x-1.6x)
    // keeps it well under 60, so this also guards against the old bug of returning
    // DateTime.Now.Minute (a clock reading unrelated to trip length).
    Assert.InRange(minutes, 8, 16);
  }

  [Fact]
  public void Calculate_ReturnsZeroDistance_WhenAtSameLocation()
  {
    // Act
    var (_, distanceKm) = RideEtaCalculator.Calculate(0, new TestEtaConfig { AverageSpeedKmh = 30 });

    // Assert
    Assert.Equal(0, distanceKm);
  }
}
