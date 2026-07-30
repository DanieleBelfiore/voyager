using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Ride.Module.Features.GetRideDetails;
using Ride.Module.Persistence;
using Xunit;
using RideEntity = Ride.Module.Entities.Ride;

namespace Ride.Tests.Features;

public class GetRideDetailsHandlerTests
{
  private static readonly Point SomePoint = new(0, 0);

  private static RideDbContext NewContext()
  {
    var options = new DbContextOptionsBuilder<RideDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    return new RideDbContext(options);
  }

  [Fact]
  public async Task Handle_ReturnsMappedDetails_WhenRideExists()
  {
    // Arrange
    await using var db = NewContext();
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), SomePoint, SomePoint);
    db.Rides.Add(ride);
    await db.SaveChangesAsync();
    var handler = new GetRideDetailsHandler(db);

    // Act
    var result = await handler.Handle(new GetRideDetails { Id = ride.Id, CallerId = ride.UserId }, CancellationToken.None);

    // Assert
    Assert.Equal(ride.Id, result.Id);
    Assert.Equal(ride.UserId, result.UserId);
    Assert.Equal(ride.DriverId, result.DriverId);
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    await using var db = NewContext();
    var handler = new GetRideDetailsHandler(db);
    var act = () => handler.Handle(new GetRideDetails { Id = Guid.NewGuid() }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("ride_not_found", ex.Message);
  }
}
