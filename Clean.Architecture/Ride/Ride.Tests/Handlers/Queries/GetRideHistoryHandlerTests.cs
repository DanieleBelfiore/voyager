using NetTopologySuite.Geometries;
using Ride.Application.CQRS.Queries;
using Ride.Application.Mapping;
using Ride.Application.Ports;
using RideEntity = Ride.Domain.Entities.Ride;
using NSubstitute;
using Xunit;

namespace Ride.Tests.Handlers.Queries;

public class GetRideHistoryHandlerTests
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
    var handler = new GetRideHistoryHandler(repository, new RideMapper());

    // Act
    var result = await handler.Handle(new GetRideHistory { UserId = userId, Take = 25, Page = 0 }, CancellationToken.None);

    // Assert
    var item = Assert.Single(result);
    Assert.Equal(ride.Id, item.Id);
  }
}
