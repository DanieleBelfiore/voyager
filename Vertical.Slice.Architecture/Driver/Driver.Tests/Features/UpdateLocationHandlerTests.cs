using Driver.Api.Entities;
using DriverEntity = Driver.Api.Entities.Driver;
using Driver.Api.Features.UpdateLocation;
using Driver.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Xunit;

namespace Driver.Tests.Features;

public class UpdateLocationHandlerTests
{
  private static DriverDbContext NewContext()
  {
    var options = new DbContextOptionsBuilder<DriverDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    return new DriverDbContext(options);
  }

  [Fact]
  public async Task Handle_UpdatesLocationAndInvalidatesCache_WhenDriverExists()
  {
    // Arrange
    await using var db = NewContext();
    var id = Guid.NewGuid();
    db.Drivers.Add(new DriverEntity(id));
    await db.SaveChangesAsync();
    var cache = new FakeCacheService();
    await cache.GetOrCreateAsync($"driver:status:{id}", () => Task.FromResult("cached"), TimeSpan.FromMinutes(1));
    var handler = new UpdateLocationHandler(db, cache);
    var location = new Point(1, 2);

    // Act
    await handler.Handle(new Voyager.Contracts.Driver.UpdateLocation { Id = id, Location = location }, CancellationToken.None);

    // Assert
    var driver = await db.Drivers.FindAsync(id);
    Assert.Equal(location, driver!.LastLocation);
    Assert.Null(await cache.GetAsync<string>($"driver:status:{id}"));
  }

  [Fact]
  public async Task Handle_Throws_WhenDriverNotFound()
  {
    // Arrange
    await using var db = NewContext();
    var handler = new UpdateLocationHandler(db, new FakeCacheService());
    var act = () => handler.Handle(new Voyager.Contracts.Driver.UpdateLocation { Id = Guid.NewGuid(), Location = new Point(0, 0) }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<KeyNotFoundException>(act);
    Assert.Equal("driver_not_found", ex.Message);
  }
}
