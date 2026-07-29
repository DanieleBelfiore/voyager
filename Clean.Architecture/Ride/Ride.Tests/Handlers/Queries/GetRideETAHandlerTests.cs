using NetTopologySuite.Geometries;
using Ride.Application.CQRS.Queries;
using Ride.Application.Ports;
using RideEntity = Ride.Domain.Entities.Ride;
using NSubstitute;
using Xunit;

namespace Ride.Tests.Handlers.Queries;

public class GetRideETAHandlerTests
{
  private readonly IRideRepository _repository = Substitute.For<IRideRepository>();
  private readonly IDriverLocationQuery _driverLocation = Substitute.For<IDriverLocationQuery>();
  private readonly IEtaConfig _config = Substitute.For<IEtaConfig>();
  private readonly GetRideETAHandler _handler;

  public GetRideETAHandlerTests()
  {
    _config.AverageSpeedKmh.Returns(30.0);
    _handler = new GetRideETAHandler(_repository, _driverLocation, _config);
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
    var result = await _handler.Handle(new GetRideETA { Id = ride.Id }, CancellationToken.None);

    // Assert
    Assert.NotNull(result.DistanceKm);
    Assert.NotNull(result.EstimatedArrivalMinutes);
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
    var result = await _handler.Handle(new GetRideETA { Id = ride.Id }, CancellationToken.None);

    // Assert
    Assert.Null(result.DistanceKm);
    Assert.Null(result.EstimatedArrivalMinutes);
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    _repository.GetByIdReadOnlyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((RideEntity?)null);
    var act = () => _handler.Handle(new GetRideETA { Id = Guid.NewGuid() }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("ride_not_found", ex.Message);
  }
}
