using Driver.Module.Entities;
using Driver.Module.Features.MarkDriverAvailability;
using Driver.Module.Persistence;
using Microsoft.EntityFrameworkCore;
using Voyager.Contracts.Driver;
using Xunit;
using DriverEntity = Driver.Module.Entities.Driver;

namespace Driver.Tests.Features;

public class MarkDriverAvailabilityHandlersTests
{
  private static DriverDbContext NewContext()
  {
    var options = new DbContextOptionsBuilder<DriverDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    return new DriverDbContext(options);
  }

  [Fact]
  public async Task MarkDriverOnRideHandler_SetsStatusToOnRide()
  {
    await using var db = NewContext();
    var driver = new DriverEntity(Guid.NewGuid());
    db.Drivers.Add(driver);
    await db.SaveChangesAsync();

    var handler = new MarkDriverOnRideHandler(db);
    await handler.Handle(new MarkDriverOnRide { DriverId = driver.Id }, CancellationToken.None);

    Assert.Equal(DriverStatus.OnRide, (await db.Drivers.FindAsync(driver.Id))!.Status);
  }

  [Fact]
  public async Task MarkDriverOnRideHandler_Throws_WhenDriverNotFound()
  {
    await using var db = NewContext();
    var handler = new MarkDriverOnRideHandler(db);
    var act = () => handler.Handle(new MarkDriverOnRide { DriverId = Guid.NewGuid() }, CancellationToken.None);

    var ex = await Assert.ThrowsAsync<KeyNotFoundException>(act);
    Assert.Equal("driver_not_found", ex.Message);
  }

  [Fact]
  public async Task MarkDriverAvailableHandler_SetsStatusToAvailable()
  {
    await using var db = NewContext();
    var driver = new DriverEntity(Guid.NewGuid());
    driver.UpdateAvailability(DriverStatus.OnRide);
    db.Drivers.Add(driver);
    await db.SaveChangesAsync();

    var handler = new MarkDriverAvailableHandler(db);
    await handler.Handle(new MarkDriverAvailable { DriverId = driver.Id }, CancellationToken.None);

    Assert.Equal(DriverStatus.Available, (await db.Drivers.FindAsync(driver.Id))!.Status);
  }

  [Fact]
  public async Task MarkDriverAvailableHandler_Throws_WhenDriverNotFound()
  {
    await using var db = NewContext();
    var handler = new MarkDriverAvailableHandler(db);
    var act = () => handler.Handle(new MarkDriverAvailable { DriverId = Guid.NewGuid() }, CancellationToken.None);

    var ex = await Assert.ThrowsAsync<KeyNotFoundException>(act);
    Assert.Equal("driver_not_found", ex.Message);
  }
}
