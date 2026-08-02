using RideEntity = Ride.Core.Domain.Ride;
using NetTopologySuite.Geometries;
using Ride.Core.Mapping;
using Ride.Core.Ports.Primary;
using Ride.Core.Ports.Secondary;
using Ride.Core.UseCases;
using NSubstitute;
using Voyager.Errors;
using Xunit;

namespace Ride.Tests.UseCases;

public class RequestRideUseCaseTests
{
  private static readonly Point Pickup = new(0, 0);
  private static readonly Point Dropoff = new(1, 1);
  private readonly IRideRepository _repository = Substitute.For<IRideRepository>();
  private readonly IRideEventPublisher _events = Substitute.For<IRideEventPublisher>();
  private readonly IDriverAvailabilityQuery _driverAvailability = Substitute.For<IDriverAvailabilityQuery>();
  private readonly RequestRideUseCase _useCase;

  public RequestRideUseCaseTests()
  {
    // Default to an available driver: every pre-existing test predates the driver check and
    // is asserting something else about the use case.
    _driverAvailability.GetAvailabilityAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
      .Returns(new DriverAvailability(true, true));

    _useCase = new RequestRideUseCase(_repository, new RideMapper(), _events, _driverAvailability);
  }

  [Fact]
  public async Task Handle_CreatesRideAndPublishesEvent_WhenNoInFlightRide()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    _repository.HasInFlightRideAsync(userId, Arg.Any<CancellationToken>()).Returns(false);

    // Act
    var result = await _useCase.Handle(new RequestRide { UserId = userId, DriverId = driverId, PickupLocation = Pickup, DropoffLocation = Dropoff }, CancellationToken.None);

    // Assert
    Assert.Equal(userId, result.UserId);
    Assert.Equal(driverId, result.DriverId);
    Assert.Equal(Ride.Core.Domain.RideStatus.Requested, result.Status);
    _repository.Received(1).Add(Arg.Any<Ride.Core.Domain.Ride>());
    await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    await _events.Received(1).NewRideRequestedAsync(result.Id, driverId, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_Throws_WhenUserHasInFlightRide()
  {
    // Arrange
    _repository.HasInFlightRideAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
    var act = () => _useCase.Handle(new RequestRide { UserId = Guid.NewGuid(), DriverId = Guid.NewGuid(), PickupLocation = Pickup, DropoffLocation = Dropoff }, CancellationToken.None);

    // Act & Assert
    await Assert.ThrowsAsync<ConflictException>(act);
    _repository.DidNotReceive().Add(Arg.Any<Ride.Core.Domain.Ride>());
  }

  [Fact]
  public async Task Handle_Throws_WhenDriverDoesNotExist()
  {{
    // Arrange: DriverId is caller-supplied, so an arbitrary GUID must not become a ride.
    _driverAvailability.GetAvailabilityAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
      .Returns(new DriverAvailability(false, false));
    var act = () => _useCase.Handle(new RequestRide { UserId = Guid.NewGuid(), DriverId = Guid.NewGuid(), PickupLocation = Pickup, DropoffLocation = Dropoff }, CancellationToken.None);

    // Act & Assert
    await Assert.ThrowsAsync<KeyNotFoundException>(act);
    _repository.DidNotReceive().Add(Arg.Any<RideEntity>());
  }}

  [Fact]
  public async Task Handle_Throws_WhenDriverIsNotAvailable()
  {{
    // Arrange: the driver exists but is already committed to someone else's trip.
    _driverAvailability.GetAvailabilityAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
      .Returns(new DriverAvailability(true, false));
    var act = () => _useCase.Handle(new RequestRide { UserId = Guid.NewGuid(), DriverId = Guid.NewGuid(), PickupLocation = Pickup, DropoffLocation = Dropoff }, CancellationToken.None);

    // Act & Assert
    await Assert.ThrowsAsync<ConflictException>(act);
    _repository.DidNotReceive().Add(Arg.Any<RideEntity>());
  }}
}
