using MediatR;
using NetTopologySuite.Geometries;
using NSubstitute;
using Ride.Core.CQRS.Queries;
using Ride.Handlers.CQRS.Queries;
using Xunit;

namespace Ride.Tests.Handlers.Queries;

public class GetRideCurrentLocationHandlerTests
{
  private readonly IMediator _mediator;
  private readonly TestApplicationDbContext _context;

  public GetRideCurrentLocationHandlerTests()
  {
    _context = TestBase.CreateTestDbContext();

    var mediatorMock = Substitute.For<IMediator>();
    _mediator = mediatorMock;

    mediatorMock.Send(Arg.Any<GetRideCurrentLocation>(), Arg.Any<CancellationToken>())
      .Returns(c => new GetRideCurrentLocationHandler(_context)
        .Handle(c.Arg<GetRideCurrentLocation>(), c.Arg<CancellationToken>()));
  }

  [Fact]
  public async Task Handle_ReturnsCurrentLocation_WhenRideExists()
  {
    // Arrange
    var rideId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    var location = new Point(5, 5);
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, UserId = userId, LastLocation = location });
    await _context.SaveChangesAsync();

    // Act
    var result = await _mediator.Send(new GetRideCurrentLocation { Id = rideId, CallerId = userId });

    // Assert
    Assert.Equal(location, result.LastLocation);
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    var act = () => _mediator.Send(new GetRideCurrentLocation { Id = Guid.NewGuid() });

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
    var act = () => _mediator.Send(new GetRideCurrentLocation { Id = rideId, CallerId = Guid.NewGuid() });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    Assert.Equal("not_ride_participant", ex.Message);
  }
}
