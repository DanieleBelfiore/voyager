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
    ride.Accept(ride.DriverId);
    repository.GetByIdAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);

    var handler = new StartRideHandler(repository);
    var startLocation = new Point(2, 2);

    await handler.Handle(new StartRide { Id = ride.Id, Location = startLocation, CallerId = ride.DriverId }, CancellationToken.None);

    Assert.Equal(Ride.Domain.Enums.RideStatus.InProgress, ride.Status);
    // Where the driver reports starting from is recorded, but it does not become the pickup:
    // PickupLocation is what the rider agreed to and what the fare is measured against, so
    // letting Start move it let a driver stretch the priced segment before the trip even began.
    Assert.Equal(new Point(0, 0), ride.PickupLocation);
    Assert.Equal(startLocation, ride.LastLocation);
    Assert.NotNull(ride.StartAt);
    await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task StartRide_ShouldThrow_WhenCallerIsNotDriver()
  {
    var repository = Substitute.For<IRideRepository>();
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), new Point(0, 0), new Point(1, 1));
    ride.Accept(ride.DriverId);
    repository.GetByIdAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);

    var handler = new StartRideHandler(repository);
    var act = () => handler.Handle(new StartRide { Id = ride.Id, Location = new Point(2, 2), CallerId = Guid.NewGuid() }, CancellationToken.None);

    await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
  }
}
