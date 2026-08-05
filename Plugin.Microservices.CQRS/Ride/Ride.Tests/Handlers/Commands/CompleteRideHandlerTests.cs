using Common.Core.Exceptions;
using Hikyaku;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.Geometries;
using NSubstitute;
using Ride.Core.CQRS.Commands;
using Ride.Core.CQRS.Events;
using Ride.Core.Enums;
using Ride.Handlers;
using Ride.Handlers.CQRS.Commands;
using Xunit;

namespace Ride.Tests.Handlers.Commands;

public class CompleteRideHandlerTests
{
  private readonly IHikyaku _mediator;
  private readonly TestApplicationDbContext _context;
  private readonly IConfiguration _configuration;

  public CompleteRideHandlerTests()
  {
    _context = TestBase.CreateTestDbContext();
    _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
      ["BaseFare"] = "2.5",
      ["PerKmRate"] = "1.2",
      ["PerMinuteRate"] = "0.25"
    }).Build();

    var mediatorMock = Substitute.For<IHikyaku>();
    _mediator = mediatorMock;

    mediatorMock.Send(Arg.Any<CompleteRide>(), Arg.Any<CancellationToken>())
      .Returns(c => new CompleteRideHandler(_context, _mediator, _configuration)
        .Handle(c.Arg<CompleteRide>(), c.Arg<CancellationToken>()));
  }

  [Fact]
  public async Task Handle_CompletesRideAndPublishesEvent_WhenRideExists()
  {
    // Arrange
    var rideId = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    var pickup = new Point(0, 0);
    var dropoff = new Point(0, 1);
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, DriverId = driverId, Status = RideStatus.InProgress, PickupLocation = pickup, DropoffLocation = dropoff, StartAt = DateTime.UtcNow.AddMinutes(-10) });
    await _context.SaveChangesAsync();

    // Act
    await _mediator.Send(new CompleteRide { Id = rideId, CallerId = driverId, Location = dropoff });

    // Assert: server computes price from the agreed route (pickup at (0,0) to dropoff at (0,1)
    // is ~111.2km) and elapsed time (~10 minutes) — never from a client-supplied value. Pin the
    // actual formula (not just "some positive amount above BaseFare") so a regression that drops
    // a term or swaps distance/duration would fail this test.
    var ride = await _context.Rides.FindAsync(rideId);
    var distanceInMeters = RideGeoCalculator.DistanceInMeters(pickup, dropoff);
    var expectedPrice = 2.5 + 1.2 * (distanceInMeters / 1000) + 0.25 * 10;
    Assert.Equal(RideStatus.Completed, ride!.Status);
    Assert.Equal(dropoff, ride.DropoffLocation);
    Assert.InRange(ride.Price!.Value, expectedPrice - 0.1, expectedPrice + 0.1);
    await _mediator.Received(1).Publish(Arg.Is<RideCompleted>(e => e.RideId == rideId), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    var act = () => _mediator.Send(new CompleteRide { Id = Guid.NewGuid(), Location = new Point(0, 0) });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<NotFoundException>(act);
    Assert.Equal("no_ride_found", ex.Message);
  }

  [Fact]
  public async Task Handle_Throws_WhenCallerIsNotAssignedDriver()
  {
    // Arrange
    var rideId = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, DriverId = driverId, Status = RideStatus.InProgress });
    await _context.SaveChangesAsync();
    var act = () => _mediator.Send(new CompleteRide { Id = rideId, CallerId = Guid.NewGuid(), Location = new Point(0, 0) });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    Assert.Equal("not_ride_participant", ex.Message);
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotInProgress()
  {
    // Arrange
    var rideId = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, DriverId = driverId, Status = RideStatus.DriverAssigned });
    await _context.SaveChangesAsync();
    var act = () => _mediator.Send(new CompleteRide { Id = rideId, CallerId = driverId, Location = new Point(0, 0) });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<ConflictException>(act);
    Assert.Equal("operation_not_permitted", ex.Message);
  }

  /// <summary>
  /// The completion coordinate is supplied by the driver, who is the party being paid. Pricing it
  /// let them name a point far past the agreed destination and charge for the difference, and
  /// overwriting DropoffLocation with it destroyed the evidence — the ride then read as though
  /// the rider had asked to go there. The fare comes off the route the rider agreed to; where the
  /// driver actually stopped is recorded on LastLocation, which is not priced.
  /// </summary>
  [Fact]
  public async Task Handle_PricesTheAgreedRoute_WhenTheDriverClaimsAFartherDropoff()
  {
    // Arrange
    var rideId = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    var pickup = new Point(0, 0);
    var agreedDropoff = new Point(0, 1);
    var claimed = new Point(0, 5);
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, DriverId = driverId, Status = RideStatus.InProgress, PickupLocation = pickup, DropoffLocation = agreedDropoff, StartAt = DateTime.UtcNow });
    await _context.SaveChangesAsync();

    // Act
    await _mediator.Send(new CompleteRide { Id = rideId, CallerId = driverId, Location = claimed });

    // Assert
    var ride = await _context.Rides.FindAsync(rideId);
    var expectedPrice = 2.5 + 1.2 * (RideGeoCalculator.DistanceInMeters(pickup, agreedDropoff) / 1000);
    Assert.InRange(ride!.Price!.Value, expectedPrice - 0.1, expectedPrice + 0.1);
    Assert.Equal(agreedDropoff, ride.DropoffLocation);
    Assert.Equal(claimed, ride.LastLocation);
  }
}
