using NetTopologySuite.Geometries;
using Ride.Core.Ports.Primary;
using Ride.Core.Ports.Secondary;
using Ride.Core.UseCases;
using RideEntity = Ride.Core.Domain.Ride;
using NSubstitute;
using Xunit;

namespace Ride.Tests.UseCases;

public class CancelRideUseCaseTests
{
  private static readonly Point SomePoint = new(0, 0);
  private readonly IRideRepository _repository = Substitute.For<IRideRepository>();
  private readonly IRideEventPublisher _events = Substitute.For<IRideEventPublisher>();
  private readonly CancelRideUseCase _useCase;

  public CancelRideUseCaseTests()
  {
    _useCase = new CancelRideUseCase(_repository, _events);
  }

  [Fact]
  public async Task Handle_CancelsRideAndPublishesEvent_WhenCancellable()
  {
    // Arrange
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), SomePoint, SomePoint);
    _repository.GetByIdAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);

    // Act
    await _useCase.Handle(new CancelRide { Id = ride.Id, CancellationReason = "changed_mind", CallerId = ride.UserId }, CancellationToken.None);

    // Assert
    Assert.Equal(Ride.Core.Domain.RideStatus.Cancelled, ride.Status);
    Assert.Equal("changed_mind", ride.CancellationReason);
    await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    await _events.Received(1).RideCancelledAsync(ride.Id, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((RideEntity?)null);
    var act = () => _useCase.Handle(new CancelRide { Id = Guid.NewGuid(), CancellationReason = "x" }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("no_ride_found", ex.Message);
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotCancellable()
  {
    // Arrange
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), SomePoint, SomePoint);
    ride.Accept(ride.DriverId);
    ride.Start(SomePoint);
    _repository.GetByIdAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);
    var act = () => _useCase.Handle(new CancelRide { Id = ride.Id, CancellationReason = "x", CallerId = ride.UserId }, CancellationToken.None);

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(act);
  }
}
