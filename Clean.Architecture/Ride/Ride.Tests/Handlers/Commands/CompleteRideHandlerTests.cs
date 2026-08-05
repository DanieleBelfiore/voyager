using NetTopologySuite.Geometries;
using Ride.Application.CQRS.Commands;
using Ride.Application.CQRS.Queries;
using Ride.Application.Ports;
using RideEntity = Ride.Domain.Entities.Ride;
using NSubstitute;
using Xunit;

namespace Ride.Tests.Handlers.Commands;

public class CompleteRideHandlerTests
{
  private static readonly Point SomePoint = new(0, 0);
  private readonly IRideRepository _repository = Substitute.For<IRideRepository>();
  private readonly IRideEventPublisher _events = Substitute.For<IRideEventPublisher>();
  private readonly IDriverAvailabilityNotifier _availability = Substitute.For<IDriverAvailabilityNotifier>();
  private readonly IFareConfig _fareConfig = Substitute.For<IFareConfig>();
  private readonly CompleteRideHandler _handler;

  public CompleteRideHandlerTests()
  {
    _fareConfig.BaseFare.Returns(2.5);
    _fareConfig.PerKmRate.Returns(1.2);
    _fareConfig.PerMinuteRate.Returns(0.25);

    _handler = new CompleteRideHandler(_repository, _events, _availability, _fareConfig);
  }

  [Fact]
  public async Task Handle_CompletesRideAndPublishesEvent_WhenRideExists()
  {
    // Arrange
    var dropoff = new Point(0, 1);
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), SomePoint, dropoff);
    ride.Accept(ride.DriverId);
    ride.Start(SomePoint);
    _repository.GetByIdAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);

    // Act
    await _handler.Handle(new CompleteRide { Id = ride.Id, Location = dropoff, CallerId = ride.DriverId }, CancellationToken.None);

    // Assert: server computes price from the agreed route (pickup (0,0) → dropoff (0,1),
    // ~111.2km) and elapsed time — never from a client-supplied value. Start() stamped StartAt
    // moments ago, so the duration term is ~0 here and the distance term dominates. Pinning the
    // arithmetic (rather than just "above BaseFare") is what makes a dropped term, a missing
    // /1000, or a distance/duration swap actually fail this test.
    var distanceKm = RideEtaCalculator.DistanceInMeters(SomePoint, dropoff) / 1000;
    var expectedPrice = 2.5 + 1.2 * distanceKm;
    Assert.Equal(Ride.Domain.Enums.RideStatus.Completed, ride.Status);
    Assert.Equal(dropoff, ride.DropoffLocation);
    Assert.InRange(ride.Price!.Value, expectedPrice - 0.1, expectedPrice + 0.1);
    await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    await _events.Received(1).RideCompletedAsync(ride.Id, Arg.Any<CancellationToken>());
    await _availability.Received(1).MarkAvailableAsync(ride.DriverId, Arg.Any<CancellationToken>());
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
    var pickup = new Point(0, 0);
    var agreedDropoff = new Point(0, 1);
    var claimed = new Point(0, 5);
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), pickup, agreedDropoff);
    ride.Accept(ride.DriverId);
    ride.Start(pickup);
    _repository.GetByIdAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);

    // Act
    await _handler.Handle(new CompleteRide { Id = ride.Id, Location = claimed, CallerId = ride.DriverId }, CancellationToken.None);

    // Assert
    var agreedKm = RideEtaCalculator.DistanceInMeters(pickup, agreedDropoff) / 1000;
    var expectedPrice = 2.5 + 1.2 * agreedKm;
    Assert.InRange(ride.Price!.Value, expectedPrice - 0.1, expectedPrice + 0.1);
    Assert.Equal(agreedDropoff, ride.DropoffLocation);
    Assert.Equal(claimed, ride.LastLocation);
  }

  /// <summary>
  /// The other end of the same hole: Start used to overwrite PickupLocation with wherever the
  /// driver said they were, so a driver could stretch the priced segment from both ends at once.
  /// </summary>
  [Fact]
  public async Task Handle_PricesTheAgreedRoute_WhenTheDriverStartedFarFromThePickup()
  {
    // Arrange
    var pickup = new Point(0, 0);
    var agreedDropoff = new Point(0, 1);
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), pickup, agreedDropoff);
    ride.Accept(ride.DriverId);
    ride.Start(new Point(0, -4));
    _repository.GetByIdAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);

    // Act
    await _handler.Handle(new CompleteRide { Id = ride.Id, Location = agreedDropoff, CallerId = ride.DriverId }, CancellationToken.None);

    // Assert
    var agreedKm = RideEtaCalculator.DistanceInMeters(pickup, agreedDropoff) / 1000;
    var expectedPrice = 2.5 + 1.2 * agreedKm;
    Assert.InRange(ride.Price!.Value, expectedPrice - 0.1, expectedPrice + 0.1);
    Assert.Equal(pickup, ride.PickupLocation);
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((RideEntity?)null);
    var act = () => _handler.Handle(new CompleteRide { Id = Guid.NewGuid(), Location = SomePoint }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<KeyNotFoundException>(act);
    Assert.Equal("no_ride_found", ex.Message);
  }

  [Fact]
  public async Task Handle_Throws_Unauthorized_WhenCallerIsNotDriver()
  {
    // Arrange
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), SomePoint, SomePoint);
    ride.Accept(ride.DriverId);
    ride.Start(SomePoint);
    _repository.GetByIdAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);
    var act = () => _handler.Handle(new CompleteRide { Id = ride.Id, Location = SomePoint, CallerId = Guid.NewGuid() }, CancellationToken.None);

    // Act & Assert
    await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
  }
}
