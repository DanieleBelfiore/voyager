using NetTopologySuite.Geometries;
using Ride.Application.CQRS.Queries;
using Ride.Application.Ports;
using RideEntity = Ride.Domain.Entities.Ride;
using NSubstitute;
using Xunit;

namespace Ride.Tests.Handlers.Queries;

public class GetRideCurrentLocationHandlerTests
{
  private static readonly Point SomePoint = new(0, 0);
  private readonly IRideRepository _repository = Substitute.For<IRideRepository>();
  private readonly GetRideCurrentLocationHandler _handler;

  public GetRideCurrentLocationHandlerTests()
  {
    _handler = new GetRideCurrentLocationHandler(_repository);
  }

  [Fact]
  public async Task Handle_ReturnsCurrentLocation_WhenRideExists()
  {
    // Arrange
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), SomePoint, SomePoint);
    ride.Accept(ride.DriverId);
    ride.Start(new Point(5, 5));
    _repository.GetByIdReadOnlyAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);

    // Act
    var result = await _handler.Handle(new GetRideCurrentLocation { Id = ride.Id, CallerId = ride.UserId }, CancellationToken.None);

    // Assert
    Assert.Equal(ride.LastLocation, result.LastLocation);
    Assert.Equal(ride.LastUpdateDate, result.LastUpdateDate);
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    _repository.GetByIdReadOnlyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((RideEntity?)null);
    var act = () => _handler.Handle(new GetRideCurrentLocation { Id = Guid.NewGuid() }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("ride_not_found", ex.Message);
  }

  [Fact]
  public async Task Handle_Throws_Unauthorized_WhenCallerNotParticipant()
  {
    // Arrange
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), SomePoint, SomePoint);
    _repository.GetByIdReadOnlyAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);
    var act = () => _handler.Handle(new GetRideCurrentLocation { Id = ride.Id, CallerId = Guid.NewGuid() }, CancellationToken.None);

    // Act & Assert
    await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
  }
}
