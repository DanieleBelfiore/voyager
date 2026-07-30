using Driver.Core.CQRS.Queries;
using Driver.Core.Dtos;
using MediatR;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.Geometries;
using NSubstitute;
using Ride.Core.CQRS.Queries;
using Ride.Handlers.CQRS.Queries;
using Xunit;

namespace Ride.Tests.Handlers.Queries;

public class GetRideETAHandlerTests
{
  private readonly IMediator _mediator;
  private readonly TestApplicationDbContext _context;
  private readonly IConfiguration _configuration;

  public GetRideETAHandlerTests()
  {
    _context = TestBase.CreateTestDbContext();
    _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
      ["AverageSpeedKmh"] = "30"
    }).Build();

    var mediatorMock = Substitute.For<IMediator>();
    _mediator = mediatorMock;

    mediatorMock.Send(Arg.Any<GetRideETA>(), Arg.Any<CancellationToken>())
      .Returns(c => new GetRideETAHandler(_context, _mediator, _configuration)
        .Handle(c.Arg<GetRideETA>(), c.Arg<CancellationToken>()));
  }

  [Fact]
  public async Task Handle_ReturnsEta_WhenDriverLocationKnown()
  {
    // Arrange
    var driverId = Guid.NewGuid();
    var rideId = Guid.NewGuid();
    var pickup = new Point(0, 0);
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, DriverId = driverId, PickupLocation = pickup });
    await _context.SaveChangesAsync();
    _mediator.Send(Arg.Any<GetDriverStatus>(), Arg.Any<CancellationToken>())
      .Returns(new DriverStatusResponse { LastLocation = new Point(0, 1) });

    // Act
    var result = await _mediator.Send(new GetRideETA { Id = rideId, CallerId = driverId });

    // Assert
    Assert.NotNull(result.DistanceKm);
    Assert.NotNull(result.EstimatedArrivalMinutes);
  }

  [Fact]
  public async Task Handle_ReturnsEmptyResponse_WhenDriverLocationUnknown()
  {
    // Arrange
    var driverId = Guid.NewGuid();
    var rideId = Guid.NewGuid();
    var pickup = new Point(0, 0);
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, DriverId = driverId, PickupLocation = pickup });
    await _context.SaveChangesAsync();
    _mediator.Send(Arg.Any<GetDriverStatus>(), Arg.Any<CancellationToken>())
      .Returns(new DriverStatusResponse { LastLocation = null });

    // Act
    var result = await _mediator.Send(new GetRideETA { Id = rideId, CallerId = driverId });

    // Assert
    Assert.Null(result.DistanceKm);
    Assert.Null(result.EstimatedArrivalMinutes);
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    var act = () => _mediator.Send(new GetRideETA { Id = Guid.NewGuid() });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("ride_not_found", ex.Message);
  }

  [Fact]
  public async Task Handle_Throws_WhenDriverNotFound()
  {
    // Arrange
    var rideId = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, DriverId = driverId });
    await _context.SaveChangesAsync();
    _mediator.Send(Arg.Any<GetDriverStatus>(), Arg.Any<CancellationToken>())
      .Returns((DriverStatusResponse?)null);
    var act = () => _mediator.Send(new GetRideETA { Id = rideId, CallerId = driverId });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("driver_not_found", ex.Message);
  }

  [Fact]
  public async Task Handle_Throws_WhenCallerIsNotRideParticipant()
  {
    // Arrange
    var rideId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, UserId = Guid.NewGuid(), DriverId = Guid.NewGuid() });
    await _context.SaveChangesAsync();
    var act = () => _mediator.Send(new GetRideETA { Id = rideId, CallerId = Guid.NewGuid() });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    Assert.Equal("not_ride_participant", ex.Message);
  }
}
