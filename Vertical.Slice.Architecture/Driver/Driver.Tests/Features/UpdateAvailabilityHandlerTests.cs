using Driver.Api.Entities;
using DriverEntity = Driver.Api.Entities.Driver;
using Driver.Api.Features.UpdateAvailability;
using Driver.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Driver.Tests.Features;

public class UpdateAvailabilityHandlerTests
{
  private static DriverDbContext NewContext()
  {
    var options = new DbContextOptionsBuilder<DriverDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    return new DriverDbContext(options);
  }

  [Fact]
  public async Task Handle_UpdatesStatus_WhenDriverExists()
  {
    // Arrange
    await using var db = NewContext();
    var id = Guid.NewGuid();
    db.Drivers.Add(new DriverEntity(id));
    await db.SaveChangesAsync();
    var handler = new UpdateAvailabilityHandler(db);

    // Act
    await handler.Handle(new UpdateAvailability { Id = id, Status = DriverStatus.OnRide }, CancellationToken.None);

    // Assert
    var driver = await db.Drivers.FindAsync(id);
    Assert.Equal(DriverStatus.OnRide, driver!.Status);
  }

  [Fact]
  public async Task Handle_Throws_WhenDriverNotFound()
  {
    // Arrange
    await using var db = NewContext();
    var handler = new UpdateAvailabilityHandler(db);
    var act = () => handler.Handle(new UpdateAvailability { Id = Guid.NewGuid(), Status = DriverStatus.Available }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("driver_not_found", ex.Message);
  }
}
