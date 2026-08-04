using Common.Core.Exceptions;
using Driver.Core.CQRS.Queries;
using Driver.Core.Dtos;
using Driver.Core.Enums;
using Hikyaku;
using NetTopologySuite.Geometries;
using NSubstitute;
using Ride.Core.CQRS.Commands;
using Ride.Core.CQRS.Events;
using Ride.Core.Enums;
using Ride.Handlers;
using Ride.Handlers.CQRS.Commands;
using Xunit;

namespace Ride.Tests.Handlers.Commands;

public class RequestRideHandlerTests
{
  private static readonly Point Pickup = new(0, 0);
  private static readonly Point Dropoff = new(1, 1);
  private readonly IHikyaku _mediator;
  private readonly TestApplicationDbContext _context;

  public RequestRideHandlerTests()
  {
    _context = TestBase.CreateTestDbContext();

    var mediatorMock = Substitute.For<IHikyaku>();
    _mediator = mediatorMock;

    // The handler now checks the driver exists and is free before creating the ride; default to
    // an available driver so the pre-existing cases keep asserting what they were written for.
    mediatorMock.Send(Arg.Any<GetDriverStatus>(), Arg.Any<CancellationToken>())
      .Returns(new DriverStatusResponse { Id = Guid.NewGuid(), Status = DriverStatus.Available });

    mediatorMock.Send(Arg.Any<RequestRide>(), Arg.Any<CancellationToken>())
      .Returns(c => new RequestRideHandler(_context, new RideMapper(), _mediator)
        .Handle(c.Arg<RequestRide>(), c.Arg<CancellationToken>()));
  }

  [Fact]
  public async Task Handle_CreatesRideAndPublishesEvent_WhenNoInFlightRide()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var driverId = Guid.NewGuid();

    // Act
    var result = await _mediator.Send(new RequestRide { UserId = userId, DriverId = driverId, PickupLocation = Pickup, DropoffLocation = Dropoff });

    // Assert
    Assert.Equal(userId, result.UserId);
    Assert.Equal(driverId, result.DriverId);
    Assert.Equal(RideStatus.Requested, result.Status);
    Assert.Single(_context.Rides);
    await _mediator.Received(1).Publish(Arg.Is<NewRideRequested>(e => e.RideId == result.Id && e.DriverId == driverId), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_Throws_WhenUserHasInFlightRide()
  {
    // Arrange
    var userId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { UserId = userId, Status = RideStatus.Requested });
    await _context.SaveChangesAsync();
    var act = () => _mediator.Send(new RequestRide { UserId = userId, DriverId = Guid.NewGuid(), PickupLocation = Pickup, DropoffLocation = Dropoff });

    // Act & Assert
    await Assert.ThrowsAsync<ConflictException>(act);
  }

  [Fact]
  public async Task Handle_Throws_WhenDriverIsNotAvailable()
  {
    // Arrange: DriverId is caller-supplied. A driver already committed to someone else's trip
    // must not have another ride pinned onto them.
    var mediatorMock = Substitute.For<IHikyaku>();
    mediatorMock.Send(Arg.Any<GetDriverStatus>(), Arg.Any<CancellationToken>())
      .Returns(new DriverStatusResponse { Id = Guid.NewGuid(), Status = DriverStatus.OnRide });

    var handler = new RequestRideHandler(_context, new RideMapper(), mediatorMock);
    var act = () => handler.Handle(new RequestRide { UserId = Guid.NewGuid(), DriverId = Guid.NewGuid(), PickupLocation = Pickup, DropoffLocation = Dropoff }, CancellationToken.None);

    // Act & Assert
    await Assert.ThrowsAsync<ConflictException>(act);
    Assert.Empty(_context.Rides);
  }
}
