using NetTopologySuite.Geometries;
using Driver.Core.Validation;
using Voyager.Errors;
using Xunit;

namespace Driver.Tests;

/// <summary>Covers the coordinate guard that Driver.Core applies to every caller-supplied location.</summary>
public class GeoGuardTests
{
  [Fact]
  public void Required_Throws_WhenPointIsMissing()
  {
    // Controllers carry no [ApiController] and nullable reference types are disabled, so a body
    // that simply omits the location bound to null and reached the handler, where writing it out
    // threw and surfaced as a 500.
    var ex = Assert.Throws<InvalidInputException>(() => GeoGuard.Required(null, "pickup_location"));

    Assert.Equal("pickup_location_required", ex.Message);
  }

  [Theory]
  // Latitude is Y, longitude is X. GeoJSON orders them [lon, lat], the opposite of how they are
  // usually spoken, so a swapped pair is the common way to end up outside the valid range.
  [InlineData(0, 91)]
  [InlineData(0, -91)]
  [InlineData(181, 0)]
  [InlineData(-181, 0)]
  [InlineData(12.4964, 419.028)]
  public void Required_Throws_WhenCoordinateIsOutOfRange(double longitude, double latitude)
  {
    // SQL Server's geography type rejects these at INSERT time, which is also a 500 — bad input
    // belongs at 400 instead.
    var ex = Assert.Throws<InvalidInputException>(() => GeoGuard.Required(new Point(longitude, latitude), "location"));

    Assert.Equal("location_out_of_range", ex.Message);
  }

  [Theory]
  [InlineData(double.NaN, 0)]
  [InlineData(0, double.NaN)]
  [InlineData(double.PositiveInfinity, 0)]
  [InlineData(0, double.NegativeInfinity)]
  public void Required_Throws_WhenCoordinateIsNotFinite(double longitude, double latitude)
  {
    var ex = Assert.Throws<InvalidInputException>(() => GeoGuard.Required(new Point(longitude, latitude), "location"));

    Assert.Equal("location_out_of_range", ex.Message);
  }

  [Theory]
  [InlineData(12.4964, 41.9028)]  // Rome
  [InlineData(0, 0)]              // Null Island is a valid coordinate, however unlikely
  [InlineData(180, 90)]           // Range bounds are inclusive
  [InlineData(-180, -90)]
  public void Required_ReturnsPoint_WhenCoordinateIsValid(double longitude, double latitude)
  {
    var point = new Point(longitude, latitude);

    Assert.Same(point, GeoGuard.Required(point, "location"));
  }
}
