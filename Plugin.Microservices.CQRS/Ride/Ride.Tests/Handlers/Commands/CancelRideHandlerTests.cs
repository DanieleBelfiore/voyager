using Common.Core.Exceptions;
using Driver.Core.CQRS.Commands;
using Driver.Core.Enums;
using MediatR;
using NSubstitute;
using Ride.Core.CQRS.Commands;
using Ride.Core.CQRS.Events;
using Ride.Core.Enums;
using Ride.Handlers.CQRS.Commands;
using Xunit;

namespace Ride.Tests.Handlers.Commands;

public class CancelRideHandlerTests
{
  private readonly IMediator _mediator;
  private readonly TestApplicationDbContext _context;

  public CancelRideHandlerTests()
  {
    _context = TestBase.CreateTestDbContext();

    var mediatorMock = Substitute.For<IMediator>();
    _mediator = mediatorMock;

    mediatorMock.Send(Arg.Any<CancelRide>(), Arg.Any<CancellationToken>())
      .Returns(c => new CancelRideHandler(_context, _mediator)
        .Handle(c.Arg<CancelRide>(), c.Arg<CancellationToken>()));
  }

  [Fact]
  public async Task Handle_CancelsRideAndPublishesEvent_WhenCancellable()
  {
    // Arrange
    var rideId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, UserId = userId, Status = RideStatus.Requested });
    await _context.SaveChangesAsync();

    // Act
    await _mediator.Send(new CancelRide { Id = rideId, CallerId = userId, CancellationReason = "changed_mind" });

    // Assert
    var ride = await _context.Rides.FindAsync(rideId);
    Assert.Equal(RideStatus.Cancelled, ride!.Status);
    Assert.Equal("changed_mind", ride.CancellationReason);
    await _mediator.Received(1).Publish(Arg.Is<RideCancelled>(e => e.RideId == rideId), Arg.Any<CancellationToken>());
    // Still at Requested: this ride never moved the driver to OnRide, so cancelling it must not
    // release a driver who may be mid-trip on someone else's ride.
    await _mediator.DidNotReceive().Send(Arg.Any<UpdateAvailability>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_ReleasesDriver_WhenRideWasDriverAssigned()
  {
    // Arrange
    var rideId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, UserId = userId, DriverId = driverId, Status = RideStatus.DriverAssigned });
    await _context.SaveChangesAsync();

    // Act
    await _mediator.Send(new CancelRide { Id = rideId, CallerId = userId, CancellationReason = "changed_mind" });

    // Assert
    var ride = await _context.Rides.FindAsync(rideId);
    Assert.Equal(RideStatus.Cancelled, ride!.Status);
    await _mediator.Received(1).Send(
      Arg.Is<UpdateAvailability>(c => c.Id == driverId && c.Status == DriverStatus.Available),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    var act = () => _mediator.Send(new CancelRide { Id = Guid.NewGuid(), CancellationReason = "x" });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<NotFoundException>(act);
    Assert.Equal("no_ride_found", ex.Message);
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotCancellable()
  {
    // Arrange
    var rideId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, UserId = userId, Status = RideStatus.InProgress });
    await _context.SaveChangesAsync();
    var act = () => _mediator.Send(new CancelRide { Id = rideId, CallerId = userId, CancellationReason = "x" });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<ConflictException>(act);
    Assert.Equal("operation_not_permitted", ex.Message);
  }

  [Fact]
  public async Task Handle_Throws_WhenCallerIsNotRideParticipant()
  {
    // Arrange
    var rideId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, UserId = Guid.NewGuid(), DriverId = Guid.NewGuid(), Status = RideStatus.Requested });
    await _context.SaveChangesAsync();
    var act = () => _mediator.Send(new CancelRide { Id = rideId, CallerId = Guid.NewGuid(), CancellationReason = "x" });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    Assert.Equal("not_ride_participant", ex.Message);
  }
}
