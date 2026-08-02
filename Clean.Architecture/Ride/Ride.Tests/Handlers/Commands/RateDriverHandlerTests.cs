using Voyager.Errors;
using NetTopologySuite.Geometries;
using Ride.Application.CQRS.Commands;
using Ride.Application.Ports;
using RideEntity = Ride.Domain.Entities.Ride;
using NSubstitute;
using Xunit;

namespace Ride.Tests.Handlers.Commands;

public class RateDriverHandlerTests
{
  private static readonly Point SomePoint = new(0, 0);
  private readonly IRideRepository _repository = Substitute.For<IRideRepository>();
  private readonly IRatingUpdateService _ratings = Substitute.For<IRatingUpdateService>();
  private readonly IRideEventPublisher _events = Substitute.For<IRideEventPublisher>();
  private readonly RateDriverHandler _handler;

  public RateDriverHandlerTests()
  {
    _handler = new RateDriverHandler(_repository, _ratings, _events);
  }

  [Fact]
  public async Task Handle_UpdatesRatingAndPublishesEventForRatedRide()
  {
    // Arrange: the notification must always use the ride being rated (request.RideId), not
    // derived from the driver's history — Identity now self-tracks the ratings count, so the
    // handler no longer needs to look up or pass a rides count at all.
    var driverId = Guid.NewGuid();
    var ride = CompletedRide(Guid.NewGuid(), driverId);
    _repository.GetByIdAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);

    // Act
    await _handler.Handle(new RateDriver { RideId = ride.Id, Rating = 5, CallerId = ride.UserId }, CancellationToken.None);

    // Assert
    await _ratings.Received(1).UpdateRatingAsync(driverId, 5, Arg.Any<CancellationToken>());
    await _events.Received(1).DriverRatingReceivedAsync(ride.Id, 5, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((RideEntity?)null);
    var act = () => _handler.Handle(new RateDriver { RideId = Guid.NewGuid(), Rating = 3 }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<KeyNotFoundException>(act);
    Assert.Equal("ride_not_found", ex.Message);
  }

  [Fact]
  public async Task Handle_Throws_Unauthorized_WhenCallerIsNotRider()
  {
    // Arrange
    var driverId = Guid.NewGuid();
    var ride = CompletedRide(Guid.NewGuid(), driverId);
    _repository.GetByIdAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);
    var act = () => _handler.Handle(new RateDriver { RideId = ride.Id, Rating = 5, CallerId = driverId }, CancellationToken.None);

    // Act & Assert
    await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
  }

  // Rating is only legal on a ride that actually happened, so walk the real transitions rather
  // than reaching past the aggregate to force a status.
  private static RideEntity CompletedRide(Guid userId, Guid driverId)
  {
    var ride = new RideEntity(userId, driverId, SomePoint, SomePoint);
    ride.Accept(driverId);
    ride.Start(SomePoint);
    ride.Complete(SomePoint, 10);

    return ride;
  }

  [Fact]
  public async Task Handle_Throws_WhenRideIsNotCompleted()
  {
    // Arrange: otherwise a rider could request a ride and immediately rate the driver down
    // without ever taking it.
    var driverId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    var ride = new RideEntity(userId, driverId, SomePoint, SomePoint);
    _repository.GetByIdAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);
    var act = () => _handler.Handle(new RateDriver { RideId = ride.Id, Rating = 5, CallerId = userId }, CancellationToken.None);

    // Act & Assert
    await Assert.ThrowsAsync<ConflictException>(act);
    await _ratings.DidNotReceive().UpdateRatingAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
  }

  [Theory]
  [InlineData(0)]
  [InlineData(6)]
  [InlineData(int.MaxValue)]
  public async Task Handle_Throws_WhenRatingIsOutOfRange(int rating)
  {
    // Arrange: the value feeds a running average in Identity, so an out-of-range rating
    // permanently skews the target's score and the matching rank built on it.
    var driverId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    var ride = CompletedRide(userId, driverId);
    _repository.GetByIdAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);
    var act = () => _handler.Handle(new RateDriver { RideId = ride.Id, Rating = rating, CallerId = userId }, CancellationToken.None);

    // Act & Assert
    await Assert.ThrowsAsync<InvalidInputException>(act);
    await _ratings.DidNotReceive().UpdateRatingAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_Throws_WhenRideWasAlreadyRated()
  {
    // Arrange: without a persisted marker the same ride could be rated repeatedly, and each
    // replay moves the average again.
    var driverId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    var ride = CompletedRide(userId, driverId);
    _repository.GetByIdAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);
    await _handler.Handle(new RateDriver { RideId = ride.Id, Rating = 5, CallerId = userId }, CancellationToken.None);

    var act = () => _handler.Handle(new RateDriver { RideId = ride.Id, Rating = 5, CallerId = userId }, CancellationToken.None);

    // Act & Assert
    await Assert.ThrowsAsync<ConflictException>(act);
    await _ratings.Received(1).UpdateRatingAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
  }
}
