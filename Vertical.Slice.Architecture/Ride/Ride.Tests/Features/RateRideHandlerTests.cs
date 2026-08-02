using Voyager.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NSubstitute;
using Ride.Api.Features.RateRide;
using Ride.Api.Persistence;
using Voyager.Contracts.Identity;
using Voyager.Contracts.Ride;
using Xunit;
using RideEntity = Ride.Api.Entities.Ride;

namespace Ride.Tests.Features;

public class RateRideHandlerTests
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
  public async Task Handle_UpdatesRiderRatingAndPublishesEvent_WhenCalledByDriver()
  {
    // Arrange: RateRide is the driver rating the rider (see SendToRiderNewRateReceived) —
    // the caller must be the ride's driver, and the rating is applied to the rider (UserId).
    await using var db = NewContext();
    var userId = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    var ride = new RideEntity(userId, driverId, SomePoint, SomePoint);
    ride.Accept(driverId);
    ride.Start(SomePoint);
    ride.Complete(SomePoint, 10);
    db.Rides.Add(ride);
    await db.SaveChangesAsync();
    var mediator = Substitute.For<IMediator>();
    var handler = new RateRideHandler(db, mediator);

    // Act
    await handler.Handle(new RateRide { RideId = ride.Id, Rating = 4, CallerId = driverId }, CancellationToken.None);

    // Assert
    await mediator.Received(1).Send(Arg.Is<UpdateUserRating>(c => c.UserId == userId && c.Rating == 4), Arg.Any<CancellationToken>());
    await mediator.Received(1).Publish(Arg.Is<RiderRatingReceived>(e => e.RideId == ride.Id && e.Rating == 4), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    await using var db = NewContext();
    var handler = new RateRideHandler(db, Substitute.For<IMediator>());
    var act = () => handler.Handle(new RateRide { RideId = Guid.NewGuid(), Rating = 3 }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<KeyNotFoundException>(act);
    Assert.Equal("ride_not_found", ex.Message);
  }

  [Fact]
  public async Task Handle_Throws_Unauthorized_WhenCallerIsNotDriver()
  {
    // Arrange: the rider (or anyone else) attempting to call RateRide must be rejected —
    // only the ride's driver may rate the rider.
    await using var db = NewContext();
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), SomePoint, SomePoint);
    db.Rides.Add(ride);
    await db.SaveChangesAsync();
    var handler = new RateRideHandler(db, Substitute.For<IMediator>());
    var act = () => handler.Handle(new RateRide { RideId = ride.Id, Rating = 4, CallerId = ride.UserId }, CancellationToken.None);

    // Act & Assert
    await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
  }

  [Fact]
  public async Task Handle_Throws_WhenRideIsNotCompleted()
  {
    // Arrange: otherwise a rider could request a ride and immediately rate the driver down
    // without ever taking it.
    await using var db = NewContext();
    var userId = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    var ride = new RideEntity(userId, driverId, SomePoint, SomePoint);
    db.Rides.Add(ride);
    await db.SaveChangesAsync();
    var mediator = Substitute.For<IMediator>();
    var handler = new RateRideHandler(db, mediator);
    var act = () => handler.Handle(new RateRide { RideId = ride.Id, Rating = 5, CallerId = driverId }, CancellationToken.None);

    // Act & Assert
    await Assert.ThrowsAsync<ConflictException>(act);
    await mediator.DidNotReceive().Send(Arg.Any<UpdateUserRating>(), Arg.Any<CancellationToken>());
  }

  [Theory]
  [InlineData(0)]
  [InlineData(6)]
  [InlineData(int.MaxValue)]
  public async Task Handle_Throws_WhenRatingIsOutOfRange(int rating)
  {
    // Arrange: the value feeds a running average in Identity, so an out-of-range rating
    // permanently skews the target's score and the matching rank built on it.
    await using var db = NewContext();
    var userId = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    db.Rides.Add(CompletedRide(userId, driverId));
    await db.SaveChangesAsync();
    var ride = db.Rides.Single();
    var mediator = Substitute.For<IMediator>();
    var handler = new RateRideHandler(db, mediator);
    var act = () => handler.Handle(new RateRide { RideId = ride.Id, Rating = rating, CallerId = driverId }, CancellationToken.None);

    // Act & Assert
    await Assert.ThrowsAsync<InvalidInputException>(act);
    await mediator.DidNotReceive().Send(Arg.Any<UpdateUserRating>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_Throws_WhenRideWasAlreadyRated()
  {
    // Arrange: without a persisted marker the same ride could be rated repeatedly, and each
    // replay moves the average again.
    await using var db = NewContext();
    var userId = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    db.Rides.Add(CompletedRide(userId, driverId));
    await db.SaveChangesAsync();
    var ride = db.Rides.Single();
    var mediator = Substitute.For<IMediator>();
    var handler = new RateRideHandler(db, mediator);
    await handler.Handle(new RateRide { RideId = ride.Id, Rating = 5, CallerId = driverId }, CancellationToken.None);

    var act = () => handler.Handle(new RateRide { RideId = ride.Id, Rating = 1, CallerId = driverId }, CancellationToken.None);

    // Act & Assert
    await Assert.ThrowsAsync<ConflictException>(act);
    await mediator.Received(1).Send(Arg.Any<UpdateUserRating>(), Arg.Any<CancellationToken>());
  }

  private static RideEntity CompletedRide(Guid userId, Guid driverId)
  {
    var ride = new RideEntity(userId, driverId, SomePoint, SomePoint);
    ride.Accept(driverId);
    ride.Start(SomePoint);
    ride.Complete(SomePoint, 10);

    return ride;
  }
}
