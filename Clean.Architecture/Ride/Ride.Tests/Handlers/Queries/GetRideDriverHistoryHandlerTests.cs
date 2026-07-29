using NetTopologySuite.Geometries;
using Ride.Application.CQRS.Queries;
using Ride.Application.Mapping;
using Ride.Application.Ports;
using RideEntity = Ride.Domain.Entities.Ride;
using NSubstitute;
using Xunit;

namespace Ride.Tests.Handlers.Queries;

public class GetRideDriverHistoryHandlerTests
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
    var handler = new GetRideDriverHistoryHandler(repository, new RideMapper());

    // Act
    var result = await handler.Handle(new GetRideDriverHistory { DriverId = driverId, Take = 25, Page = 0 }, CancellationToken.None);

    // Assert
    var item = Assert.Single(result);
    Assert.Equal(ride.Id, item.Id);
  }
}
