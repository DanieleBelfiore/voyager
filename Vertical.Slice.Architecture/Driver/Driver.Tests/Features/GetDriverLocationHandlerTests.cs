using Driver.Api.Features.GetDriverLocation;
using Driver.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Voyager.Contracts.Driver;
using Xunit;
using DriverEntity = Driver.Api.Entities.Driver;

namespace Driver.Tests.Features;

public class GetDriverLocationHandlerTests
{
  private static DriverDbContext NewDb() => new(new DbContextOptionsBuilder<DriverDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString())
    .Options);

  [Fact]
  public async Task GetDriverLocation_ShouldReturnLastKnownLocation()
  {
    await using var db = NewDb();
    var id = Guid.NewGuid();
    var location = new Point(9.19, 45.46) { SRID = 4326 };
    var driver = new DriverEntity(id);
    driver.UpdateLocation(location);
    db.Drivers.Add(driver);
    await db.SaveChangesAsync();

    var result = await new GetDriverLocationHandler(db).Handle(new GetDriverLocation { DriverId = id }, CancellationToken.None);

    Assert.NotNull(result);
    Assert.Equal(location, result.LastLocation);
  }

  [Fact]
  public async Task GetDriverLocation_ShouldReturnNullLocation_WhenDriverIsUnknown()
  {
    // An unknown DriverId is the caller's bad request to handle, not a fault here, so this
    // mirrors GetDriverAvailabilityHandler and reports absence instead of throwing.
    await using var db = NewDb();

    var result = await new GetDriverLocationHandler(db).Handle(new GetDriverLocation { DriverId = Guid.NewGuid() }, CancellationToken.None);

    Assert.NotNull(result);
    Assert.Null(result.LastLocation);
  }

  [Fact]
  public async Task GetDriverLocation_ShouldReturnNullLocation_WhenDriverNeverReportedOne()
  {
    await using var db = NewDb();
    var id = Guid.NewGuid();
    db.Drivers.Add(new DriverEntity(id));
    await db.SaveChangesAsync();

    var result = await new GetDriverLocationHandler(db).Handle(new GetDriverLocation { DriverId = id }, CancellationToken.None);

    Assert.NotNull(result);
    Assert.Null(result.LastLocation);
  }
}
