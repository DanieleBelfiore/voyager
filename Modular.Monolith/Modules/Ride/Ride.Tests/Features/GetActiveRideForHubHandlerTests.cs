using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Ride.Module.Features.GetActiveRideForHub;
using Ride.Module.Persistence;
using SharedGetActiveRide = Voyager.Contracts.Ride.GetActiveRide;
using Xunit;
using RideEntity = Ride.Module.Entities.Ride;

namespace Ride.Tests.Features;

public class GetActiveRideForHubHandlerTests
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
  public async Task Handle_ReturnsActiveRideInfo_WhenRideExists()
  {
    // Arrange
    await using var db = NewContext();
    var driverId = Guid.NewGuid();
    var ride = new RideEntity(Guid.NewGuid(), driverId, SomePoint, SomePoint);
    ride.Accept(driverId);
    db.Rides.Add(ride);
    await db.SaveChangesAsync();
    var handler = new GetActiveRideForHubHandler(db);

    // Act
    var result = await handler.Handle(new SharedGetActiveRide { DriverId = driverId }, CancellationToken.None);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(ride.Id, result.Id);
    Assert.Equal(ride.PickupLocation, result.PickupLocation);
  }

  [Fact]
  public async Task Handle_ReturnsNull_WhenNoActiveRide()
  {
    // Arrange
    await using var db = NewContext();
    var handler = new GetActiveRideForHubHandler(db);

    // Act
    var result = await handler.Handle(new SharedGetActiveRide { DriverId = Guid.NewGuid() }, CancellationToken.None);

    // Assert
    Assert.Null(result);
  }
}
