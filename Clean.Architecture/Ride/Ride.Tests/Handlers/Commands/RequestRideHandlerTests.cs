using NetTopologySuite.Geometries;
using Ride.Application.CQRS.Commands;
using Ride.Application.Mapping;
using Ride.Application.Ports;
using NSubstitute;
using Xunit;

namespace Ride.Tests.Handlers.Commands;

public class RequestRideHandlerTests
{
  private static readonly Point Pickup = new(0, 0);
  private static readonly Point Dropoff = new(1, 1);
  private readonly IRideRepository _repository = Substitute.For<IRideRepository>();
  private readonly IRideEventPublisher _events = Substitute.For<IRideEventPublisher>();
  private readonly RequestRideHandler _handler;

  public RequestRideHandlerTests()
  {
    _handler = new RequestRideHandler(_repository, new RideMapper(), _events);
  }

  [Fact]
  public async Task Handle_CreatesRideAndPublishesEvent_WhenNoInFlightRide()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    _repository.HasInFlightRideAsync(userId, Arg.Any<CancellationToken>()).Returns(false);

    // Act
    var result = await _handler.Handle(new RequestRide { UserId = userId, DriverId = driverId, PickupLocation = Pickup, DropoffLocation = Dropoff }, CancellationToken.None);

    // Assert
    Assert.Equal(userId, result.UserId);
    Assert.Equal(driverId, result.DriverId);
    Assert.Equal(Ride.Domain.Enums.RideStatus.Requested, result.Status);
    _repository.Received(1).Add(Arg.Any<Ride.Domain.Entities.Ride>());
    await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    await _events.Received(1).NewRideRequestedAsync(result.Id, driverId, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_Throws_WhenUserHasInFlightRide()
  {
    // Arrange
    _repository.HasInFlightRideAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
    var act = () => _handler.Handle(new RequestRide { UserId = Guid.NewGuid(), DriverId = Guid.NewGuid(), PickupLocation = Pickup, DropoffLocation = Dropoff }, CancellationToken.None);

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(act);
    _repository.DidNotReceive().Add(Arg.Any<Ride.Domain.Entities.Ride>());
  }
}
