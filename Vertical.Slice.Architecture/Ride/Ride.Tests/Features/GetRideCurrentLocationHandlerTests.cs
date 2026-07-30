using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Ride.Api.Features.GetRideCurrentLocation;
using Ride.Api.Persistence;
using Xunit;
using RideEntity = Ride.Api.Entities.Ride;

namespace Ride.Tests.Features;

public class GetRideCurrentLocationHandlerTests
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
  public async Task Handle_ReturnsCurrentLocation_WhenRideExists()
  {
    // Arrange
    await using var db = NewContext();
    var driverId = Guid.NewGuid();
    var ride = new RideEntity(Guid.NewGuid(), driverId, SomePoint, SomePoint);
    ride.Accept(driverId);
    ride.Start(new Point(5, 5));
    db.Rides.Add(ride);
    await db.SaveChangesAsync();
    var handler = new GetRideCurrentLocationHandler(db);

    // Act
    var result = await handler.Handle(new GetRideCurrentLocation { Id = ride.Id, CallerId = ride.UserId }, CancellationToken.None);

    // Assert
    Assert.Equal(ride.LastLocation, result.LastLocation);
    Assert.Equal(ride.LastUpdateDate, result.LastUpdateDate);
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    await using var db = NewContext();
    var handler = new GetRideCurrentLocationHandler(db);
    var act = () => handler.Handle(new GetRideCurrentLocation { Id = Guid.NewGuid() }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("ride_not_found", ex.Message);
  }
}
