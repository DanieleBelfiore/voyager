using MediatR;
using NSubstitute;
using Ride.Core.CQRS.Queries;
using Ride.Handlers;
using Ride.Handlers.CQRS.Queries;
using Xunit;

namespace Ride.Tests.Handlers.Queries;

public class GetRideDetailsHandlerTests
{
  private readonly IMediator _mediator;
  private readonly TestApplicationDbContext _context;

  public GetRideDetailsHandlerTests()
  {
    _context = TestBase.CreateTestDbContext();

    var mediatorMock = Substitute.For<IMediator>();
    _mediator = mediatorMock;

    mediatorMock.Send(Arg.Any<GetRideDetails>(), Arg.Any<CancellationToken>())
      .Returns(c => new GetRideDetailsHandler(_context, new RideMapper())
        .Handle(c.Arg<GetRideDetails>(), c.Arg<CancellationToken>()));
  }

  [Fact]
  public async Task Handle_ReturnsMappedDetails_WhenRideExists()
  {
    // Arrange
    var rideId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, UserId = userId, DriverId = driverId });
    await _context.SaveChangesAsync();

    // Act
    var result = await _mediator.Send(new GetRideDetails { Id = rideId, CallerId = userId });

    // Assert
    Assert.Equal(rideId, result.Id);
    Assert.Equal(userId, result.UserId);
    Assert.Equal(driverId, result.DriverId);
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    var act = () => _mediator.Send(new GetRideDetails { Id = Guid.NewGuid() });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("ride_not_found", ex.Message);
  }

  [Fact]
  public async Task Handle_Throws_WhenCallerIsNotRideParticipant()
  {
    // Arrange
    var rideId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, UserId = Guid.NewGuid(), DriverId = Guid.NewGuid() });
    await _context.SaveChangesAsync();
    var act = () => _mediator.Send(new GetRideDetails { Id = rideId, CallerId = Guid.NewGuid() });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    Assert.Equal("not_ride_participant", ex.Message);
  }
}
