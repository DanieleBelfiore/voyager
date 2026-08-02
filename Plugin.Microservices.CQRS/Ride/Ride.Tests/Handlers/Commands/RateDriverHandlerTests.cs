using Common.Core.Exceptions;
using Identity.Core.CQRS.Commands;
using MediatR;
using NSubstitute;
using Ride.Core.CQRS.Commands;
using Ride.Core.CQRS.Events;
using Ride.Handlers.CQRS.Commands;
using Xunit;

namespace Ride.Tests.Handlers.Commands;

public class RateDriverHandlerTests
{
  private readonly IMediator _mediator;
  private readonly TestApplicationDbContext _context;

  public RateDriverHandlerTests()
  {
    _context = TestBase.CreateTestDbContext();

    var mediatorMock = Substitute.For<IMediator>();
    _mediator = mediatorMock;

    mediatorMock.Send(Arg.Any<RateDriver>(), Arg.Any<CancellationToken>())
      .Returns(c => new RateDriverHandler(_context, _mediator)
        .Handle(c.Arg<RateDriver>(), c.Arg<CancellationToken>()));
  }

  [Fact]
  public async Task Handle_UpdatesRatingAndPublishesEvent_ForRatedRide()
  {
    // Arrange: the notification must always use the ride being rated (request.RideId), not
    // derived from the driver's history — Identity now self-tracks the ratings count, so the
    // handler no longer needs to look up or pass a rides count at all.
    var userId = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    var rideId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, UserId = userId, DriverId = driverId, Status = Ride.Core.Enums.RideStatus.Completed });
    await _context.SaveChangesAsync();

    // Act
    await _mediator.Send(new RateDriver { RideId = rideId, CallerId = userId, Rating = 5 });

    // Assert
    await _mediator.Received(1).Send(Arg.Is<UpdateUserRating>(c => c.UserId == driverId && c.Rating == 5), Arg.Any<CancellationToken>());
    await _mediator.Received(1).Publish(Arg.Is<DriverRatingReceived>(e => e.RideId == rideId && e.Rating == 5), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    var act = () => _mediator.Send(new RateDriver { RideId = Guid.NewGuid(), Rating = 3 });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<NotFoundException>(act);
    Assert.Equal("ride_not_found", ex.Message);
  }

  [Fact]
  public async Task Handle_Throws_WhenCallerIsNotRideOwner()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    var rideId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, UserId = userId, DriverId = driverId });
    await _context.SaveChangesAsync();
    var act = () => _mediator.Send(new RateDriver { RideId = rideId, CallerId = Guid.NewGuid(), Rating = 5 });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    Assert.Equal("not_ride_participant", ex.Message);
  }

  [Fact]
  public async Task Handle_Throws_WhenRideIsNotCompleted()
  {
    // Arrange: otherwise a rider could request a ride and immediately rate the driver down
    // without ever taking it.
    var userId = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    var rideId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, UserId = userId, DriverId = driverId, Status = Ride.Core.Enums.RideStatus.Requested });
    await _context.SaveChangesAsync();

    var handler = new RateDriverHandler(_context, _mediator);
    var act = () => handler.Handle(new RateDriver { RideId = rideId, Rating = 5, CallerId = userId }, CancellationToken.None);

    // Act & Assert
    await Assert.ThrowsAsync<ConflictException>(act);
    await _mediator.DidNotReceive().Send(Arg.Any<UpdateUserRating>(), Arg.Any<CancellationToken>());
  }

  [Theory]
  [InlineData(0)]
  [InlineData(6)]
  [InlineData(int.MaxValue)]
  public async Task Handle_Throws_WhenRatingIsOutOfRange(int rating)
  {
    // Arrange: the value feeds a running average in Identity, so an out-of-range rating
    // permanently skews the target's score and the matching rank built on it.
    var userId = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    var rideId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, UserId = userId, DriverId = driverId, Status = Ride.Core.Enums.RideStatus.Completed });
    await _context.SaveChangesAsync();

    var handler = new RateDriverHandler(_context, _mediator);
    var act = () => handler.Handle(new RateDriver { RideId = rideId, Rating = rating, CallerId = userId }, CancellationToken.None);

    // Act & Assert
    await Assert.ThrowsAsync<InvalidInputException>(act);
    await _mediator.DidNotReceive().Send(Arg.Any<UpdateUserRating>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_Throws_WhenRideWasAlreadyRated()
  {
    // Arrange: without a persisted marker the same ride could be rated repeatedly, and each
    // replay moves the average again.
    var userId = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    var rideId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, UserId = userId, DriverId = driverId, Status = Ride.Core.Enums.RideStatus.Completed });
    await _context.SaveChangesAsync();

    var handler = new RateDriverHandler(_context, _mediator);
    await handler.Handle(new RateDriver { RideId = rideId, Rating = 5, CallerId = userId }, CancellationToken.None);

    var act = () => handler.Handle(new RateDriver { RideId = rideId, Rating = 1, CallerId = userId }, CancellationToken.None);

    // Act & Assert
    await Assert.ThrowsAsync<ConflictException>(act);
    await _mediator.Received(1).Send(Arg.Any<UpdateUserRating>(), Arg.Any<CancellationToken>());
  }
}
