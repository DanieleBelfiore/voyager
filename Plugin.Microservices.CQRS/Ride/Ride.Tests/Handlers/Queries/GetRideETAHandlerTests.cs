using Common.Core.Exceptions;
using Driver.Core.CQRS.Queries;
using Driver.Core.Dtos;
using Hikyaku;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.Geometries;
using NSubstitute;
using Ride.Core.CQRS.Queries;
using Ride.Handlers.CQRS.Queries;
using Xunit;

namespace Ride.Tests.Handlers.Queries;

public class GetRideETAHandlerTests
{
  private readonly IHikyaku _mediator;
  private readonly TestApplicationDbContext _context;
  private readonly IConfiguration _configuration;

  public GetRideETAHandlerTests()
  {
    _context = TestBase.CreateTestDbContext();
    // All four time-of-day multipliers must be set: GetValue<double> silently returns 0 for a
    // missing key, which used to make this test's outcome depend on the UTC hour it happened to
    // run in (whichever multiplier bucket the wall clock fell into that day).
    _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
      ["AverageSpeedKmh"] = "30",
      ["MorningPeakMultiplier"] = "1.5",
      ["EveningPeakMultiplier"] = "1.6",
      ["NightMultiplier"] = "0.8",
      ["LunchMultiplier"] = "1.2"
    }).Build();

    var mediatorMock = Substitute.For<IHikyaku>();
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

    // Assert: pickup and driver location are 1 degree of latitude apart (~111.2km great-circle).
    // At 30km/h that's ~222 base minutes before the 0.8x-1.6x time-of-day multiplier — asserting
    // real ranges (not just NotNull) also catches DistanceKm silently holding meters instead of
    // kilometers (would show as ~111195 here, well outside this range).
    Assert.InRange(result.DistanceKm!.Value, 111.0, 111.3);
    Assert.InRange(result.EstimatedArrivalMinutes!.Value, 170, 360);
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
    var ex = await Assert.ThrowsAsync<NotFoundException>(act);
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
    var ex = await Assert.ThrowsAsync<NotFoundException>(act);
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
