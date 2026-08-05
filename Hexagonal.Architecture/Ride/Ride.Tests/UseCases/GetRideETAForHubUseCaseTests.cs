using NetTopologySuite.Geometries;
using Ride.Core.Ports.Secondary;
using Ride.Core.UseCases;
using RideEntity = Ride.Core.Domain.Ride;
using SharedGetRideETA = Voyager.Contracts.Ride.GetRideETA;
using NSubstitute;
using Xunit;

namespace Ride.Tests.UseCases;

public class GetRideETAForHubUseCaseTests
{
  private readonly IRideRepository _repository = Substitute.For<IRideRepository>();
  private readonly IDriverLocationQuery _driverLocation = Substitute.For<IDriverLocationQuery>();
  private readonly IEtaConfig _config = Substitute.For<IEtaConfig>();
  private readonly GetRideETAForHubUseCase _useCase;

  public GetRideETAForHubUseCaseTests()
  {
    _config.AverageSpeedKmh.Returns(30.0);
    _useCase = new GetRideETAForHubUseCase(_repository, _driverLocation, _config);
  }

  [Fact]
  public async Task Handle_ReturnsEta_WhenDriverLocationKnown()
  {
    // Arrange
    var pickup = new Point(0, 0);
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), pickup, pickup);
    _repository.GetByIdReadOnlyAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);
    _driverLocation.GetLocationAsync(ride.DriverId, Arg.Any<CancellationToken>()).Returns(new Point(0, 1));

    // Act
    var result = await _useCase.Handle(new SharedGetRideETA { Id = ride.Id, CallerId = ride.DriverId }, CancellationToken.None);

    // Assert
    Assert.NotNull(result.DistanceKm);
  }

  [Fact]
  public async Task Handle_ReturnsEmptyResponse_WhenDriverLocationUnknown()
  {
    // Arrange
    var pickup = new Point(0, 0);
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), pickup, pickup);
    _repository.GetByIdReadOnlyAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);
    _driverLocation.GetLocationAsync(ride.DriverId, Arg.Any<CancellationToken>()).Returns((Point?)null);

    // Act
    var result = await _useCase.Handle(new SharedGetRideETA { Id = ride.Id, CallerId = ride.DriverId }, CancellationToken.None);

    // Assert
    Assert.Null(result.DistanceKm);
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    _repository.GetByIdReadOnlyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((RideEntity?)null);
    var act = () => _useCase.Handle(new SharedGetRideETA { Id = Guid.NewGuid() }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<KeyNotFoundException>(act);
    Assert.Equal("ride_not_found", ex.Message);
  }

  /// <summary>
  /// Reachable only over the broker, which is exactly why it needs the check: without it any
  /// caller that can put a message on the bus could ask for any ride by id and learn where that
  /// driver is. Same rule as the local GET /rides/{id}/eta twin — participants only.
  /// </summary>
  [Fact]
  public async Task Handle_Throws_WhenCallerIsNotARideParticipant()
  {
    // Arrange
    var pickup = new Point(0, 0);
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), pickup, pickup);
    _repository.GetByIdReadOnlyAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);

    // Act & Assert
    await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
      _useCase.Handle(new SharedGetRideETA { Id = ride.Id, CallerId = Guid.NewGuid() }, CancellationToken.None));

    await _driverLocation.DidNotReceive().GetLocationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
  }
}
