using Driver.Api.Features.GetDriverStatus;
using Driver.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Driver.Tests.Features;

public class GetDriverStatusHandlerTests
{
  [Fact]
  public async Task GetDriverStatus_ShouldUseCacheOnSecondCall()
  {
    var options = new DbContextOptionsBuilder<DriverDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var db = new DriverDbContext(options);
    var id = Guid.NewGuid();
    var driver = new Driver.Api.Entities.Driver(id);
    db.Drivers.Add(driver);
    await db.SaveChangesAsync();

    var cache = new FakeCacheService();
    var handler = new GetDriverStatusHandler(db, cache);

    var first = await handler.Handle(new GetDriverStatus { Id = id, CallerId = id }, CancellationToken.None);

    driver.UpdateAvailability(Driver.Api.Entities.DriverStatus.OnRide); // shouldn't affect cached result

    var second = await handler.Handle(new GetDriverStatus { Id = id, CallerId = id }, CancellationToken.None);

    Assert.Equivalent(first, second);
    Assert.Equal(Driver.Api.Entities.DriverStatus.Available, second.Status);
  }

  [Fact]
  public async Task GetDriverStatus_ShouldRejectACallerReadingAnotherDriver()
  {
    await using var db = new DriverDbContext(new DbContextOptionsBuilder<DriverDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    var id = Guid.NewGuid();
    db.Drivers.Add(new Driver.Api.Entities.Driver(id));
    await db.SaveChangesAsync();

    var handler = new GetDriverStatusHandler(db, new FakeCacheService());

    await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
      handler.Handle(new GetDriverStatus { Id = id, CallerId = Guid.NewGuid() }, CancellationToken.None));
  }

  [Fact]
  public async Task GetDriverStatus_ShouldNotServeAForeignCallerFromAWarmCache()
  {
    // The driver's own read populates the cache first, so this fails if the ownership check sits
    // inside the cache factory rather than in front of it.
    await using var db = new DriverDbContext(new DbContextOptionsBuilder<DriverDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    var id = Guid.NewGuid();
    db.Drivers.Add(new Driver.Api.Entities.Driver(id));
    await db.SaveChangesAsync();

    var handler = new GetDriverStatusHandler(db, new FakeCacheService());
    await handler.Handle(new GetDriverStatus { Id = id, CallerId = id }, CancellationToken.None);

    await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
      handler.Handle(new GetDriverStatus { Id = id, CallerId = Guid.NewGuid() }, CancellationToken.None));
  }
}
