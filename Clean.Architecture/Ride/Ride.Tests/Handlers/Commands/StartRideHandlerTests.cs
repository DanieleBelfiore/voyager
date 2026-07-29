using NetTopologySuite.Geometries;
using Ride.Application.CQRS.Commands;
using Ride.Application.Ports;
using RideEntity = Ride.Domain.Entities.Ride;
using NSubstitute;
using Xunit;

namespace Ride.Tests.Handlers.Commands;

public class StartRideHandlerTests
{
  [Fact]
  public async Task StartRide_ShouldSetInProgressAndLocation()
  {
    var repository = Substitute.For<IRideRepository>();
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), new Point(0, 0), new Point(1, 1));
    repository.GetByIdAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);

    var handler = new StartRideHandler(repository);
    var startLocation = new Point(2, 2);

    await handler.Handle(new StartRide { Id = ride.Id, Location = startLocation }, CancellationToken.None);

    Assert.Equal(Ride.Domain.Enums.RideStatus.InProgress, ride.Status);
    Assert.Equal(startLocation, ride.PickupLocation);
    Assert.Equal(startLocation, ride.LastLocation);
    Assert.NotNull(ride.StartAt);
    await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }
}
