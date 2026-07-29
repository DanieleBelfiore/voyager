using NetTopologySuite.Geometries;
using Ride.Core.Mapping;
using Ride.Core.Ports.Primary;
using Ride.Core.Ports.Secondary;
using Ride.Core.UseCases;
using RideEntity = Ride.Core.Domain.Ride;
using NSubstitute;
using Xunit;

namespace Ride.Tests.UseCases;

public class GetRideDriverHistoryUseCaseTests
{
  private static readonly Point SomePoint = new(0, 0);

  [Fact]
  public async Task Handle_ReturnsMappedHistory()
  {
    // Arrange
    var repository = Substitute.For<IRideRepository>();
    var driverId = Guid.NewGuid();
    var ride = new RideEntity(Guid.NewGuid(), driverId, SomePoint, SomePoint);
    repository.GetDriverHistoryAsync(driverId, 25, 0, Arg.Any<CancellationToken>()).Returns([ride]);
    var useCase = new GetRideDriverHistoryUseCase(repository, new RideMapper());

    // Act
    var result = await useCase.Handle(new GetRideDriverHistory { DriverId = driverId, Take = 25, Page = 0 }, CancellationToken.None);

    // Assert
    var item = Assert.Single(result);
    Assert.Equal(ride.Id, item.Id);
  }
}
