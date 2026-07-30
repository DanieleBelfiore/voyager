using NetTopologySuite.Geometries;
using Ride.Core.Ports.Primary;
using Ride.Core.Ports.Secondary;
using Ride.Core.UseCases;
using RideEntity = Ride.Core.Domain.Ride;
using NSubstitute;
using Xunit;

namespace Ride.Tests.UseCases;

public class RateDriverUseCaseTests
{
  private static readonly Point SomePoint = new(0, 0);
  private readonly IRideRepository _repository = Substitute.For<IRideRepository>();
  private readonly IRatingUpdateService _ratings = Substitute.For<IRatingUpdateService>();
  private readonly IRideEventPublisher _events = Substitute.For<IRideEventPublisher>();
  private readonly RateDriverUseCase _useCase;

  public RateDriverUseCaseTests()
  {
    _useCase = new RateDriverUseCase(_repository, _ratings, _events);
  }

  [Fact]
  public async Task Handle_UpdatesRatingAndPublishesEvent_WhenDriverHasRides()
  {
    // Arrange
    var driverId = Guid.NewGuid();
    var ride = new RideEntity(Guid.NewGuid(), driverId, SomePoint, SomePoint);
    _repository.GetByIdReadOnlyAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);
    _repository.GetDriverHistoryAsync(driverId, -1, 0, Arg.Any<CancellationToken>()).Returns([ride]);

    // Act
    await _useCase.Handle(new RateDriver { RideId = ride.Id, Rating = 5, CallerId = ride.UserId }, CancellationToken.None);

    // Assert
    await _ratings.Received(1).UpdateRatingAsync(driverId, 5, 1, Arg.Any<CancellationToken>());
    await _events.Received(1).DriverRatingReceivedAsync(ride.Id, 5, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_DoesNotPublishEvent_WhenDriverHasNoRides()
  {
    // Arrange
    var driverId = Guid.NewGuid();
    var ride = new RideEntity(Guid.NewGuid(), driverId, SomePoint, SomePoint);
    _repository.GetByIdReadOnlyAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);
    _repository.GetDriverHistoryAsync(driverId, -1, 0, Arg.Any<CancellationToken>()).Returns([]);

    // Act
    await _useCase.Handle(new RateDriver { RideId = ride.Id, Rating = 5, CallerId = ride.UserId }, CancellationToken.None);

    // Assert
    await _events.DidNotReceive().DriverRatingReceivedAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    _repository.GetByIdReadOnlyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((RideEntity?)null);
    var act = () => _useCase.Handle(new RateDriver { RideId = Guid.NewGuid(), Rating = 3 }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("ride_not_found", ex.Message);
  }
}
