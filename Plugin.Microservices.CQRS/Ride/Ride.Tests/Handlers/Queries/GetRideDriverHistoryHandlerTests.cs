using MediatR;
using NSubstitute;
using Ride.Core.CQRS.Queries;
using Ride.Core.Enums;
using Ride.Handlers;
using Ride.Handlers.CQRS.Queries;
using Xunit;

namespace Ride.Tests.Handlers.Queries;

public class GetRideDriverHistoryHandlerTests
{
  [Fact]
  public async Task Handle_ReturnsCompletedRidesForDriver()
  {
    // Arrange
    var context = TestBase.CreateTestDbContext();
    var driverId = Guid.NewGuid();
    var rideId = Guid.NewGuid();
    context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, DriverId = driverId, Status = RideStatus.Completed });
    await context.SaveChangesAsync();

    var mediatorMock = Substitute.For<IMediator>();
    mediatorMock.Send(Arg.Any<GetRideDriverHistory>(), Arg.Any<CancellationToken>())
      .Returns(c => new GetRideDriverHistoryHandler(context, new RideMapper())
        .Handle(c.Arg<GetRideDriverHistory>(), c.Arg<CancellationToken>()));

    // Act
    var result = await mediatorMock.Send(new GetRideDriverHistory { DriverId = driverId, Take = 25, Page = 0 });

    // Assert
    var item = Assert.Single(result);
    Assert.Equal(rideId, item.Id);
  }
}
