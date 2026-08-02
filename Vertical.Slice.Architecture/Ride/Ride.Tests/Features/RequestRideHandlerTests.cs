using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NSubstitute;
using Ride.Api.Features.RequestRide;
using Ride.Api.Persistence;
using Voyager.Contracts.Ride;
using Voyager.Errors;
using Xunit;
using RideStatus = Ride.Api.Entities.RideStatus;
using Voyager.Contracts.Driver;

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
    // The handler now checks the driver exists and is free before creating the ride; default
    // to an available driver so the pre-existing cases keep asserting what they were for.
    mediator.Send(Arg.Any<GetDriverAvailability>(), Arg.Any<CancellationToken>())
      .Returns(new DriverAvailabilityInfo { Exists = true, IsAvailable = true });
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
    await mediator.Received(1).Publish(Arg.Is<NewRideRequested>(e => e.RideId == result.Id && e.DriverId == driverId), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_Throws_WhenUserHasInFlightRide()
  {
    // Arrange
    await using var db = NewContext();
    var userId = Guid.NewGuid();
    db.Rides.Add(new Ride.Api.Entities.Ride(userId, Guid.NewGuid(), Pickup, Dropoff));
    await db.SaveChangesAsync();
    var mediator = Substitute.For<IMediator>();
    mediator.Send(Arg.Any<GetDriverAvailability>(), Arg.Any<CancellationToken>())
      .Returns(new DriverAvailabilityInfo { Exists = true, IsAvailable = true });
    var handler = new RequestRideHandler(db, mediator);
    var act = () => handler.Handle(new RequestRide { UserId = userId, DriverId = Guid.NewGuid(), PickupLocation = Pickup, DropoffLocation = Dropoff }, CancellationToken.None);

    // Act & Assert
    await Assert.ThrowsAsync<ConflictException>(act);
  }

  [Fact]
  public async Task Handle_Throws_WhenDriverDoesNotExist()
  {
    // Arrange: DriverId is caller-supplied, so an arbitrary GUID must not become a ride.
    await using var db = NewContext();
    var mediator = Substitute.For<IMediator>();
    mediator.Send(Arg.Any<GetDriverAvailability>(), Arg.Any<CancellationToken>())
      .Returns(new DriverAvailabilityInfo { Exists = false, IsAvailable = false });
    var handler = new RequestRideHandler(db, mediator);
    var act = () => handler.Handle(new RequestRide { UserId = Guid.NewGuid(), DriverId = Guid.NewGuid(), PickupLocation = Pickup, DropoffLocation = Dropoff }, CancellationToken.None);

    // Act & Assert
    await Assert.ThrowsAsync<KeyNotFoundException>(act);
    Assert.Empty(db.Rides);
  }

  [Fact]
  public async Task Handle_Throws_WhenDriverIsNotAvailable()
  {
    // Arrange: the driver exists but is already committed to someone else's trip.
    await using var db = NewContext();
    var mediator = Substitute.For<IMediator>();
    mediator.Send(Arg.Any<GetDriverAvailability>(), Arg.Any<CancellationToken>())
      .Returns(new DriverAvailabilityInfo { Exists = true, IsAvailable = false });
    var handler = new RequestRideHandler(db, mediator);
    var act = () => handler.Handle(new RequestRide { UserId = Guid.NewGuid(), DriverId = Guid.NewGuid(), PickupLocation = Pickup, DropoffLocation = Dropoff }, CancellationToken.None);

    // Act & Assert
    await Assert.ThrowsAsync<ConflictException>(act);
    Assert.Empty(db.Rides);
  }
}
