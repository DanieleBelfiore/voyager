using NetTopologySuite.Geometries;
using Ride.Core.Ports.Primary;
using Ride.Core.Ports.Secondary;
using Ride.Core.UseCases;
using RideEntity = Ride.Core.Domain.Ride;
using NSubstitute;
using Xunit;

namespace Ride.Tests.UseCases;

public class RateRideUseCaseTests
{
  private static readonly Point SomePoint = new(0, 0);
  private readonly IRideRepository _repository = Substitute.For<IRideRepository>();
  private readonly IRatingUpdateService _ratings = Substitute.For<IRatingUpdateService>();
  private readonly IRideEventPublisher _events = Substitute.For<IRideEventPublisher>();
  private readonly RateRideUseCase _useCase;

  public RateRideUseCaseTests()
  {
    _useCase = new RateRideUseCase(_repository, _ratings, _events);
  }

  [Fact]
  public async Task Handle_UpdatesRatingAndPublishesEvent()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var ride = new RideEntity(userId, Guid.NewGuid(), SomePoint, SomePoint);
    _repository.GetByIdReadOnlyAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);
    _repository.GetUserHistoryAsync(userId, -1, 0, Arg.Any<CancellationToken>()).Returns([ride]);

    // Act
    await _useCase.Handle(new RateRide { RideId = ride.Id, Rating = 4, CallerId = userId }, CancellationToken.None);

    // Assert
    await _ratings.Received(1).UpdateRatingAsync(userId, 4, 1, Arg.Any<CancellationToken>());
    await _events.Received(1).RiderRatingReceivedAsync(ride.Id, 4, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    _repository.GetByIdReadOnlyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((RideEntity?)null);
    var act = () => _useCase.Handle(new RateRide { RideId = Guid.NewGuid(), Rating = 3 }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("ride_not_found", ex.Message);
  }
}
