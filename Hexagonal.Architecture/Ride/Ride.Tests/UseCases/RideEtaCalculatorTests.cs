using Ride.Core.UseCases;
using Xunit;

namespace Ride.Tests.UseCases;

public class RideEtaCalculatorTests
{
  [Fact]
  public void Calculate_RoundsDistanceToTwoDecimals()
  {
    // Act
    var (_, distanceKm) = RideEtaCalculator.Calculate(1234.5678, 30);

    // Assert
    Assert.Equal(1234.57, distanceKm);
  }

  [Fact]
  public void Calculate_ReturnsMinuteOfHour_ForEstimatedArrival()
  {
    // Act
    var (minutes, _) = RideEtaCalculator.Calculate(5000, 30);

    // Assert
    Assert.InRange(minutes, 0, 59);
  }

  [Fact]
  public void Calculate_ReturnsZeroDistance_WhenAtSameLocation()
  {
    // Act
    var (_, distanceKm) = RideEtaCalculator.Calculate(0, 30);

    // Assert
    Assert.Equal(0, distanceKm);
  }
}
