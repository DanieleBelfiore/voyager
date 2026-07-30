using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Ride.Module.Features.GetRideDriverHistory;
using Ride.Module.Persistence;
using Xunit;
using RideEntity = Ride.Module.Entities.Ride;

namespace Ride.Tests.Features;

public class GetRideDriverHistoryHandlerTests
{
  private static readonly Point SomePoint = new(0, 0);

  [Fact]
  public async Task Handle_ReturnsCompletedRidesForDriver()
  {
    // Arrange
    var options = new DbContextOptionsBuilder<RideDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    await using var db = new RideDbContext(options);
    var driverId = Guid.NewGuid();
    var ride = new RideEntity(Guid.NewGuid(), driverId, SomePoint, SomePoint);
    ride.Accept(driverId);
    ride.Start(SomePoint);
    ride.Complete(SomePoint, 10);
    db.Rides.Add(ride);
    await db.SaveChangesAsync();
    var handler = new GetRideDriverHistoryHandler(db);

    // Act
    var result = await handler.Handle(new GetRideDriverHistory { DriverId = driverId, Take = 25, Page = 0 }, CancellationToken.None);

    // Assert
    var item = Assert.Single(result);
    Assert.Equal(ride.Id, item.Id);
  }
}
