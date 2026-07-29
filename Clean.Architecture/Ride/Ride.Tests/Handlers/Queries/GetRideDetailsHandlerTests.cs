using NetTopologySuite.Geometries;
using Ride.Application.CQRS.Queries;
using Ride.Application.Mapping;
using Ride.Application.Ports;
using RideEntity = Ride.Domain.Entities.Ride;
using NSubstitute;
using Xunit;

namespace Ride.Tests.Handlers.Queries;

public class GetRideDetailsHandlerTests
{
  private static readonly Point SomePoint = new(0, 0);
  private readonly IRideRepository _repository = Substitute.For<IRideRepository>();
  private readonly GetRideDetailsHandler _handler;

  public GetRideDetailsHandlerTests()
  {
    _handler = new GetRideDetailsHandler(_repository, new RideMapper());
  }

  [Fact]
  public async Task Handle_ReturnsMappedDetails_WhenRideExists()
  {
    // Arrange
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), SomePoint, SomePoint);
    _repository.GetByIdReadOnlyAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);

    // Act
    var result = await _handler.Handle(new GetRideDetails { Id = ride.Id }, CancellationToken.None);

    // Assert
    Assert.Equal(ride.Id, result.Id);
    Assert.Equal(ride.UserId, result.UserId);
    Assert.Equal(ride.DriverId, result.DriverId);
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    _repository.GetByIdReadOnlyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((RideEntity?)null);
    var act = () => _handler.Handle(new GetRideDetails { Id = Guid.NewGuid() }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("ride_not_found", ex.Message);
  }
}
