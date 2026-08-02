using Common.Core.Exceptions;
using MediatR;
using NSubstitute;
using Ride.Core.CQRS.Commands;
using Ride.Core.CQRS.Events;
using Ride.Core.Enums;
using Ride.Handlers.CQRS.Commands;
using Xunit;

namespace Ride.Tests.Handlers.Commands;

public class AcceptRideHandlerTests
{
  private readonly IMediator _mediator;
  private readonly TestApplicationDbContext _context;

  public AcceptRideHandlerTests()
  {
    _context = TestBase.CreateTestDbContext();

    var mediatorMock = Substitute.For<IMediator>();
    _mediator = mediatorMock;

    mediatorMock.Send(Arg.Any<AcceptRide>(), Arg.Any<CancellationToken>())
      .Returns(c => new AcceptRideHandler(_context, _mediator)
        .Handle(c.Arg<AcceptRide>(), c.Arg<CancellationToken>()));
  }

  [Fact]
  public async Task AcceptRideTestFact()
  {
    // Arrange: the accepting driver must be the one the rider assigned at RequestRide time.
    var rideId = Guid.NewGuid();
    var driverId = Guid.NewGuid();

    _context.Rides.Add(new Ride.Handlers.Models.Ride
    {
      Id = rideId,
      DriverId = driverId,
      Status = RideStatus.Requested
    });

    await _context.SaveChangesAsync();

    var request = new AcceptRide
    {
      RideId = rideId,
      DriverId = driverId
    };

    // Act
    await _mediator.Send(request);

    // Assert
    var result = await _context.Rides.FindAsync(rideId);

    Assert.NotNull(result);
    Assert.Equal(RideStatus.DriverAssigned, result.Status);
    Assert.Equal(driverId, result.DriverId);

    await _mediator.Received(1).Publish(Arg.Is<RideAccepted>(e => e.RideId == rideId), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotRequested()
  {
    // Arrange
    var rideId = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, DriverId = driverId, Status = RideStatus.DriverAssigned });
    await _context.SaveChangesAsync();
    var act = () => _mediator.Send(new AcceptRide { RideId = rideId, DriverId = driverId });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<ConflictException>(act);
    Assert.Equal("operation_not_permitted", ex.Message);
  }

  [Fact]
  public async Task Handle_Throws_WhenCallerIsNotAssignedDriver()
  {
    // Arrange: a different driver must not be able to hijack a ride assigned to someone else.
    var rideId = Guid.NewGuid();
    var assignedDriverId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, DriverId = assignedDriverId, Status = RideStatus.Requested });
    await _context.SaveChangesAsync();
    var act = () => _mediator.Send(new AcceptRide { RideId = rideId, DriverId = Guid.NewGuid() });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    Assert.Equal("not_ride_participant", ex.Message);
  }
}
