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

    var first = await handler.Handle(new GetDriverStatus { Id = id }, CancellationToken.None);

    driver.UpdateAvailability(Driver.Api.Entities.DriverStatus.OnRide); // shouldn't affect cached result

    var second = await handler.Handle(new GetDriverStatus { Id = id }, CancellationToken.None);

    Assert.Equivalent(first, second);
    Assert.Equal(Driver.Api.Entities.DriverStatus.Available, second.Status);
  }
}
