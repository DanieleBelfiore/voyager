using Common.Core.Exceptions;
using Identity.Core.CQRS.Commands;
using MediatR;
using NSubstitute;
using Ride.Core.CQRS.Commands;
using Ride.Core.CQRS.Events;
using Ride.Handlers.CQRS.Commands;
using Xunit;

namespace Ride.Tests.Handlers.Commands;

public class RateRideHandlerTests
{
  private readonly IMediator _mediator;
  private readonly TestApplicationDbContext _context;

  public RateRideHandlerTests()
  {
    _context = TestBase.CreateTestDbContext();

    var mediatorMock = Substitute.For<IMediator>();
    _mediator = mediatorMock;

    mediatorMock.Send(Arg.Any<RateRide>(), Arg.Any<CancellationToken>())
      .Returns(c => new RateRideHandler(_context, _mediator)
        .Handle(c.Arg<RateRide>(), c.Arg<CancellationToken>()));
  }

  [Fact]
  public async Task Handle_UpdatesRiderRatingAndPublishesEvent_WhenCalledByDriver()
  {
    // Arrange: RateRide is the driver rating the rider (see SendToRiderNewRateReceived) —
    // the caller must be the ride's driver, and the rating is applied to the rider (UserId).
    var userId = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    var rideId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, UserId = userId, DriverId = driverId, Status = Ride.Core.Enums.RideStatus.Completed });
    await _context.SaveChangesAsync();

    // Act
    await _mediator.Send(new RateRide { RideId = rideId, CallerId = driverId, Rating = 4 });

    // Assert
    await _mediator.Received(1).Send(Arg.Is<UpdateUserRating>(c => c.UserId == userId && c.Rating == 4), Arg.Any<CancellationToken>());
    await _mediator.Received(1).Publish(Arg.Is<RiderRatingReceived>(e => e.RideId == rideId && e.Rating == 4), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    var act = () => _mediator.Send(new RateRide { RideId = Guid.NewGuid(), Rating = 3 });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<NotFoundException>(act);
    Assert.Equal("ride_not_found", ex.Message);
  }

  [Fact]
  public async Task Handle_Throws_WhenCallerIsNotDriver()
  {
    // Arrange: the rider (or anyone else) attempting to call RateRide must be rejected —
    // only the ride's driver may rate the rider.
    var userId = Guid.NewGuid();
    var rideId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, UserId = userId, DriverId = Guid.NewGuid() });
    await _context.SaveChangesAsync();
    var act = () => _mediator.Send(new RateRide { RideId = rideId, CallerId = userId, Rating = 4 });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    Assert.Equal("not_ride_participant", ex.Message);
  }
}
