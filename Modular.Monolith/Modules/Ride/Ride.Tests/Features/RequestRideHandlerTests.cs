using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NSubstitute;
using Ride.Module.Features.RequestRide;
using Ride.Module.Persistence;
using Voyager.Contracts.Ride;
using Xunit;
using RideStatus = Ride.Module.Entities.RideStatus;

namespace Ride.Tests.Features;

public class RequestRideHandlerTests
{
  private static readonly Point Pickup = new(0, 0);
  private static readonly Point Dropoff = new(1, 1);

  private static RideDbContext NewContext()
  {
    var options = new DbContextOptionsBuilder<RideDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    return new RideDbContext(options);
  }

  [Fact]
  public async Task Handle_CreatesRideAndPublishesEvent_WhenNoInFlightRide()
  {
    // Arrange
    await using var db = NewContext();
    var mediator = Substitute.For<IMediator>();
    var handler = new RequestRideHandler(db, mediator);
    var userId = Guid.NewGuid();
    var driverId = Guid.NewGuid();

    // Act
    var result = await handler.Handle(new RequestRide { UserId = userId, DriverId = driverId, PickupLocation = Pickup, DropoffLocation = Dropoff }, CancellationToken.None);

    // Assert
    Assert.Equal(userId, result.UserId);
    Assert.Equal(driverId, result.DriverId);
    Assert.Equal(RideStatus.Requested, result.Status);
    Assert.Single(db.Rides);
    await mediator.Received(1).Publish(Arg.Is<NewRideRequested>(e => e.RideId == result.Id), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_Throws_WhenUserHasInFlightRide()
  {
    // Arrange
    await using var db = NewContext();
    var userId = Guid.NewGuid();
    db.Rides.Add(new Ride.Module.Entities.Ride(userId, Guid.NewGuid(), Pickup, Dropoff));
    await db.SaveChangesAsync();
    var handler = new RequestRideHandler(db, Substitute.For<IMediator>());
    var act = () => handler.Handle(new RequestRide { UserId = userId, DriverId = Guid.NewGuid(), PickupLocation = Pickup, DropoffLocation = Dropoff }, CancellationToken.None);

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(act);
  }
}
