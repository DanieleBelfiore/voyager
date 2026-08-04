using Hikyaku;
using NSubstitute;
using Ride.Core.CQRS.Queries;
using Ride.Core.Enums;
using Ride.Handlers;
using Ride.Handlers.CQRS.Queries;
using Xunit;

namespace Ride.Tests.Handlers.Queries;

public class GetRideHistoryHandlerTests
{
  [Fact]
  public async Task Handle_ReturnsCompletedRidesForUser()
  {
    // Arrange
    var context = TestBase.CreateTestDbContext();
    var userId = Guid.NewGuid();
    var rideId = Guid.NewGuid();
    context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, UserId = userId, Status = RideStatus.Completed });
    await context.SaveChangesAsync();

    var mediatorMock = Substitute.For<IHikyaku>();
    mediatorMock.Send(Arg.Any<GetRideHistory>(), Arg.Any<CancellationToken>())
      .Returns(c => new GetRideHistoryHandler(context, new RideMapper())
        .Handle(c.Arg<GetRideHistory>(), c.Arg<CancellationToken>()));

    // Act
    var result = await mediatorMock.Send(new GetRideHistory { UserId = userId, Take = 25, Page = 0 });

    // Assert
    var item = Assert.Single(result);
    Assert.Equal(rideId, item.Id);
  }
}
