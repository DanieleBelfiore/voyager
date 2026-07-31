using NetTopologySuite.Geometries;
using Ride.Core.Ports.Primary;
using Ride.Core.Ports.Secondary;
using Ride.Core.UseCases;
using RideEntity = Ride.Core.Domain.Ride;
using NSubstitute;
using Xunit;

namespace Ride.Tests.UseCases;

public class GetRideETAUseCaseTests
{
  private readonly IRideRepository _repository = Substitute.For<IRideRepository>();
  private readonly IDriverLocationQuery _driverLocation = Substitute.For<IDriverLocationQuery>();
  private readonly IEtaConfig _config = Substitute.For<IEtaConfig>();
  private readonly GetRideETAUseCase _useCase;

  public GetRideETAUseCaseTests()
  {
    _config.AverageSpeedKmh.Returns(30.0);
    _useCase = new GetRideETAUseCase(_repository, _driverLocation, _config);
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
    var result = await _useCase.Handle(new GetRideETA { Id = ride.Id, CallerId = ride.UserId }, CancellationToken.None);

    // Assert: pickup and driver location are 1 degree of latitude apart (~111.2km great-circle).
    // At 30km/h that's ~222 base minutes before the 0.8x-1.6x time-of-day multiplier — asserting
    // real ranges (not just NotNull) is what would have caught the regression where this returned
    // DateTime.Now.Minute (a value in [0, 59] unrelated to distance/speed) instead.
    Assert.InRange(result.DistanceKm!.Value, 111.0, 111.3);
    Assert.InRange(result.EstimatedArrivalMinutes!.Value, 170, 360);
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
    var result = await _useCase.Handle(new GetRideETA { Id = ride.Id, CallerId = ride.UserId }, CancellationToken.None);

    // Assert
    Assert.Null(result.DistanceKm);
    Assert.Null(result.EstimatedArrivalMinutes);
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    _repository.GetByIdReadOnlyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((RideEntity?)null);
    var act = () => _useCase.Handle(new GetRideETA { Id = Guid.NewGuid() }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("ride_not_found", ex.Message);
  }
}
