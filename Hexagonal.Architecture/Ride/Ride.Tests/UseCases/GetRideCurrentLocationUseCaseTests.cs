using NetTopologySuite.Geometries;
using Ride.Core.Ports.Primary;
using Ride.Core.Ports.Secondary;
using Ride.Core.UseCases;
using RideEntity = Ride.Core.Domain.Ride;
using NSubstitute;
using Xunit;

namespace Ride.Tests.UseCases;

public class GetRideCurrentLocationUseCaseTests
{
  private static readonly Point SomePoint = new(0, 0);
  private readonly IRideRepository _repository = Substitute.For<IRideRepository>();
  private readonly GetRideCurrentLocationUseCase _useCase;

  public GetRideCurrentLocationUseCaseTests()
  {
    _useCase = new GetRideCurrentLocationUseCase(_repository);
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
    var result = await _useCase.Handle(new GetRideCurrentLocation { Id = ride.Id, CallerId = ride.UserId }, CancellationToken.None);

    // Assert
    Assert.Equal(ride.LastLocation, result.LastLocation);
    Assert.Equal(ride.LastUpdateDate, result.LastUpdateDate);
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    _repository.GetByIdReadOnlyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((RideEntity?)null);
    var act = () => _useCase.Handle(new GetRideCurrentLocation { Id = Guid.NewGuid() }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<KeyNotFoundException>(act);
    Assert.Equal("ride_not_found", ex.Message);
  }
}
