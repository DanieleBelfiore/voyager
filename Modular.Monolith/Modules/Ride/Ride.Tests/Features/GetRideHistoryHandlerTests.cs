using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Ride.Module.Features.GetRideHistory;
using Ride.Module.Persistence;
using Xunit;
using RideEntity = Ride.Module.Entities.Ride;

namespace Ride.Tests.Features;

public class GetRideHistoryHandlerTests
{
  private static readonly Point SomePoint = new(0, 0);

  [Fact]
  public async Task Handle_ReturnsCompletedRidesForUser()
  {
    // Arrange
    var options = new DbContextOptionsBuilder<RideDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    await using var db = new RideDbContext(options);
    var userId = Guid.NewGuid();
    var ride = new RideEntity(userId, Guid.NewGuid(), SomePoint, SomePoint);
    ride.Start(SomePoint);
    ride.Complete(SomePoint, 10);
    db.Rides.Add(ride);
    await db.SaveChangesAsync();
    var handler = new GetRideHistoryHandler(db);

    // Act
    var result = await handler.Handle(new GetRideHistory { UserId = userId, Take = 25, Page = 0 }, CancellationToken.None);

    // Assert
    var item = Assert.Single(result);
    Assert.Equal(ride.Id, item.Id);
  }
}
