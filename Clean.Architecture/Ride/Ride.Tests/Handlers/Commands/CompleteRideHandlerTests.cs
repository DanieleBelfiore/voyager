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
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), SomePoint, SomePoint);
    ride.Accept(ride.DriverId);
    ride.Start(SomePoint);
    var dropoff = new Point(0, 1);
    _repository.GetByIdAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);

    // Act
    await _handler.Handle(new CompleteRide { Id = ride.Id, Location = dropoff, CallerId = ride.DriverId }, CancellationToken.None);

    // Assert: server computes price from distance (pickup (0,0) → dropoff (0,1), ~111.2km) and
    // elapsed time — never trusts a client-supplied value. Start() stamped StartAt moments ago,
    // so the duration term is ~0 here and the distance term dominates. Pinning the arithmetic
    // (rather than just "above BaseFare") is what makes a dropped term, a missing /1000, or a
    // distance/duration swap actually fail this test.
    var distanceKm = RideEtaCalculator.DistanceInMeters(SomePoint, dropoff) / 1000;
    var expectedPrice = 2.5 + 1.2 * distanceKm;
    Assert.Equal(Ride.Domain.Enums.RideStatus.Completed, ride.Status);
    Assert.Equal(dropoff, ride.DropoffLocation);
    Assert.InRange(ride.Price!.Value, expectedPrice - 0.1, expectedPrice + 0.1);
    await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    await _events.Received(1).RideCompletedAsync(ride.Id, Arg.Any<CancellationToken>());
    await _availability.Received(1).MarkAvailableAsync(ride.DriverId, Arg.Any<CancellationToken>());
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
