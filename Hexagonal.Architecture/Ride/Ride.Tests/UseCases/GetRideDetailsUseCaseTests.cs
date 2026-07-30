using NetTopologySuite.Geometries;
using Ride.Core.Mapping;
using Ride.Core.Ports.Primary;
using Ride.Core.Ports.Secondary;
using Ride.Core.UseCases;
using RideEntity = Ride.Core.Domain.Ride;
using NSubstitute;
using Xunit;

namespace Ride.Tests.UseCases;

public class GetRideDetailsUseCaseTests
{
  private static readonly Point SomePoint = new(0, 0);
  private readonly IRideRepository _repository = Substitute.For<IRideRepository>();
  private readonly GetRideDetailsUseCase _useCase;

  public GetRideDetailsUseCaseTests()
  {
    _useCase = new GetRideDetailsUseCase(_repository, new RideMapper());
  }

  [Fact]
  public async Task Handle_ReturnsMappedDetails_WhenRideExists()
  {
    // Arrange
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), SomePoint, SomePoint);
    _repository.GetByIdReadOnlyAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);

    // Act
    var result = await _useCase.Handle(new GetRideDetails { Id = ride.Id, CallerId = ride.UserId }, CancellationToken.None);

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
    var act = () => _useCase.Handle(new GetRideDetails { Id = Guid.NewGuid() }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("ride_not_found", ex.Message);
  }
}
