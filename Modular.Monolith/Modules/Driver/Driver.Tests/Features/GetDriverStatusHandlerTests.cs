using Driver.Module.Features.GetDriverStatus;
using Driver.Module.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DriverEntity = Driver.Module.Entities.Driver;
using DriverStatus = Driver.Module.Entities.DriverStatus;

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
    var driver = new DriverEntity(id);
    db.Drivers.Add(driver);
    await db.SaveChangesAsync();

    var cache = new FakeCacheService();
    var handler = new GetDriverStatusHandler(db, cache);

    var first = await handler.Handle(new GetDriverStatus { Id = id }, CancellationToken.None);

    driver.UpdateAvailability(DriverStatus.OnRide); // shouldn't affect cached result

    var second = await handler.Handle(new GetDriverStatus { Id = id }, CancellationToken.None);

    second.Should().BeEquivalentTo(first);
    second.Status.Should().Be(DriverStatus.Available);
  }
}
