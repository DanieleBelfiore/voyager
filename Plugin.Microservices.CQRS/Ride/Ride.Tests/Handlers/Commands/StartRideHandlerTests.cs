using Common.Core.Exceptions;
using Hikyaku;
using NetTopologySuite.Geometries;
using NSubstitute;
using Ride.Core.CQRS.Commands;
using Ride.Core.Enums;
using Ride.Handlers.CQRS.Commands;
using Xunit;

namespace Ride.Tests.Handlers.Commands;

public class StartRideHandlerTests
{
  private readonly IHikyaku _mediator;
  private readonly TestApplicationDbContext _context;

  public StartRideHandlerTests()
  {
    _context = TestBase.CreateTestDbContext();

    var mediatorMock = Substitute.For<IHikyaku>();

    _mediator = mediatorMock;

    mediatorMock.Send(Arg.Any<StartRide>(), Arg.Any<CancellationToken>())
      .Returns(c => new StartRideHandler(_context)
        .Handle(c.Arg<StartRide>(), c.Arg<CancellationToken>()));
  }

  [Fact]
  public async Task StartRideTestFact()
  {
    // Arrange
    var id = Guid.NewGuid();
    var driverId = Guid.NewGuid();

    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = id, DriverId = driverId, Status = RideStatus.DriverAssigned });

    await _context.SaveChangesAsync(CancellationToken.None);

    var request = new StartRide { Id = id, CallerId = driverId, Location = new Point(12.0, 42.0) };

    // Act
    await _mediator.Send(request, CancellationToken.None);

    // Assert
    var result = await _context.Rides.FindAsync(id);

    Assert.NotNull(result);
    Assert.Equal(RideStatus.InProgress, result.Status);
  }

  [Fact]
  public async Task Handle_Throws_WhenCallerIsNotAssignedDriver()
  {
    // Arrange
    var id = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = id, DriverId = driverId, Status = RideStatus.DriverAssigned });
    await _context.SaveChangesAsync(CancellationToken.None);
    var act = () => _mediator.Send(new StartRide { Id = id, CallerId = Guid.NewGuid(), Location = new Point(12.0, 42.0) }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    Assert.Equal("not_ride_participant", ex.Message);
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotDriverAssigned()
  {
    // Arrange
    var id = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = id, DriverId = driverId, Status = RideStatus.Requested });
    await _context.SaveChangesAsync(CancellationToken.None);
    var act = () => _mediator.Send(new StartRide { Id = id, CallerId = driverId, Location = new Point(12.0, 42.0) }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<ConflictException>(act);
    Assert.Equal("operation_not_permitted", ex.Message);
  }
}
