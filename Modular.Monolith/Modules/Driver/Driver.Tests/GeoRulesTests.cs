using Driver.Module.Features.SearchBestDriver;
using Driver.Module.Features.UpdateLocation;
using NetTopologySuite.Geometries;
using Xunit;

namespace Driver.Tests;

/// <summary>
/// Exercises the shared coordinate rule (Driver.Module.Shared.GeoRules) through the two validators
/// that use it. Nothing checked these before: no [ApiController], nullable reference types
/// disabled, so a missing location bound to null and reached the handler as a 500, and an
/// out-of-range latitude reached SQL Server's geography column as another one.
/// </summary>
public class GeoRulesTests
{
  private readonly UpdateLocationValidator _updateLocation = new();
  private readonly SearchBestDriverValidator _searchBestDriver = new();

  [Fact]
  public void UpdateLocation_IsInvalid_WhenLocationIsMissing()
  {
    var result = _updateLocation.Validate(new Voyager.Contracts.Driver.UpdateLocation { Id = Guid.NewGuid() });

    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, f => f.ErrorMessage == "coordinate_required");
  }

  [Theory]
  // Latitude is Y, longitude is X. GeoJSON orders them [lon, lat], the opposite of how they are
  // usually spoken, so a swapped pair is the common way to end up outside the valid range.
  [InlineData(0, 91)]
  [InlineData(181, 0)]
  [InlineData(12.4964, 419.028)]
  [InlineData(double.NaN, 0)]
  [InlineData(0, double.PositiveInfinity)]
  public void UpdateLocation_IsInvalid_WhenCoordinateIsOutOfRange(double longitude, double latitude)
  {
    var result = _updateLocation.Validate(new Voyager.Contracts.Driver.UpdateLocation { Id = Guid.NewGuid(), Location = new Point(longitude, latitude) });

    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, f => f.ErrorMessage == "coordinate_out_of_range");
  }

  [Theory]
  [InlineData(12.4964, 41.9028)]  // Rome
  [InlineData(0, 0)]              // Null Island is a valid coordinate, however unlikely
  [InlineData(180, 90)]           // Range bounds are inclusive
  [InlineData(-180, -90)]
  public void UpdateLocation_IsValid_WhenCoordinateIsOnEarth(double longitude, double latitude)
  {
    var result = _updateLocation.Validate(new Voyager.Contracts.Driver.UpdateLocation { Id = Guid.NewGuid(), Location = new Point(longitude, latitude) });

    Assert.True(result.IsValid);
  }

  [Fact]
  public void SearchBestDriver_IsInvalid_WhenThresholdIsNotPositive()
  {
    // The threshold is the divisor for normalizedDistance in the scoring formula: at zero every
    // candidate scores NaN and the ranking collapses.
    var result = _searchBestDriver.Validate(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(12.4964, 41.9028), DistanceThresholdInMeters = 0 });

    Assert.False(result.IsValid);
  }

  [Fact]
  public void SearchBestDriver_IsValid_WhenLocationAndThresholdAreSane()
  {
    var result = _searchBestDriver.Validate(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(12.4964, 41.9028), DistanceThresholdInMeters = 5000 });

    Assert.True(result.IsValid);
  }
}
