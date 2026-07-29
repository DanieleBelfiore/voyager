using NetTopologySuite.Geometries;
using Ride.Application.CQRS.Queries;
using Ride.Application.Ports;
using RideEntity = Ride.Domain.Entities.Ride;
using SharedGetActiveRide = Voyager.Contracts.Ride.GetActiveRide;
using NSubstitute;
using Xunit;

namespace Ride.Tests.Handlers.Queries;

public class GetActiveRideForHubHandlerTests
{
  private static readonly Point SomePoint = new(0, 0);
  private readonly IRideRepository _repository = Substitute.For<IRideRepository>();
  private readonly GetActiveRideForHubHandler _handler;

  public GetActiveRideForHubHandlerTests()
  {
    _handler = new GetActiveRideForHubHandler(_repository);
  }

  [Fact]
  public async Task Handle_ReturnsActiveRideInfo_WhenRideExists()
  {
    // Arrange
    var driverId = Guid.NewGuid();
    var ride = new RideEntity(Guid.NewGuid(), driverId, SomePoint, SomePoint);
    _repository.GetActiveRideAsync(driverId, null, Arg.Any<CancellationToken>()).Returns(ride);

    // Act
    var result = await _handler.Handle(new SharedGetActiveRide { DriverId = driverId }, CancellationToken.None);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(ride.Id, result.Id);
    Assert.Equal(ride.PickupLocation, result.PickupLocation);
  }

  [Fact]
  public async Task Handle_ReturnsNull_WhenNoActiveRide()
  {
    // Arrange
    _repository.GetActiveRideAsync(Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns((RideEntity?)null);

    // Act
    var result = await _handler.Handle(new SharedGetActiveRide { DriverId = Guid.NewGuid() }, CancellationToken.None);

    // Assert
    Assert.Null(result);
  }
}
