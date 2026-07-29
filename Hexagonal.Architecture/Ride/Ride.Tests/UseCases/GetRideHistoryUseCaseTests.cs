using NetTopologySuite.Geometries;
using Ride.Core.Mapping;
using Ride.Core.Ports.Primary;
using Ride.Core.Ports.Secondary;
using Ride.Core.UseCases;
using RideEntity = Ride.Core.Domain.Ride;
using NSubstitute;
using Xunit;

namespace Ride.Tests.UseCases;

public class GetRideHistoryUseCaseTests
{
  private static readonly Point SomePoint = new(0, 0);

  [Fact]
  public async Task Handle_ReturnsMappedHistory()
  {
    // Arrange
    var repository = Substitute.For<IRideRepository>();
    var userId = Guid.NewGuid();
    var ride = new RideEntity(userId, Guid.NewGuid(), SomePoint, SomePoint);
    repository.GetUserHistoryAsync(userId, 25, 0, Arg.Any<CancellationToken>()).Returns([ride]);
    var useCase = new GetRideHistoryUseCase(repository, new RideMapper());

    // Act
    var result = await useCase.Handle(new GetRideHistory { UserId = userId, Take = 25, Page = 0 }, CancellationToken.None);

    // Assert
    var item = Assert.Single(result);
    Assert.Equal(ride.Id, item.Id);
  }
}
