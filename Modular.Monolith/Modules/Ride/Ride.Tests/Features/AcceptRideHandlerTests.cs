using Hikyaku;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NSubstitute;
using Ride.Module.Features.AcceptRide;
using Ride.Module.Persistence;
using Voyager.Contracts.Driver;
using Voyager.Contracts.Ride;
using Xunit;
using RideEntity = Ride.Module.Entities.Ride;
using RideStatus = Ride.Module.Entities.RideStatus;

namespace Ride.Tests.Features;

public class AcceptRideHandlerTests
{
  private static readonly Point SomePoint = new(0, 0);

  [Fact]
  public async Task AcceptRide_ShouldAssignDriverAndPersist()
  {
    // The accepting driver must be the one the rider assigned at RequestRide time.
    var options = new DbContextOptionsBuilder<RideDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var db = new RideDbContext(options);
    var driverId = Guid.NewGuid();
    var ride = new RideEntity(Guid.NewGuid(), driverId, SomePoint, SomePoint);
    db.Rides.Add(ride);
    await db.SaveChangesAsync();

    var mediator = Substitute.For<IHikyaku>();
    var handler = new AcceptRideHandler(db, mediator);

    await handler.Handle(new AcceptRide { RideId = ride.Id, DriverId = driverId }, CancellationToken.None);

    Assert.Equal(driverId, ride.DriverId);
    Assert.Equal(RideStatus.DriverAssigned, ride.Status);
    await mediator.Received(1).Publish(Arg.Is<RideAccepted>(e => e.RideId == ride.Id), Arg.Any<CancellationToken>());
    await mediator.Received(1).Send(Arg.Is<MarkDriverOnRide>(c => c.DriverId == driverId), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task AcceptRide_ShouldThrow_WhenCallerIsNotAssignedDriver()
  {
    // A different driver must not be able to hijack a ride assigned to someone else.
    var options = new DbContextOptionsBuilder<RideDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var db = new RideDbContext(options);
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), SomePoint, SomePoint);
    db.Rides.Add(ride);
    await db.SaveChangesAsync();

    var mediator = Substitute.For<IHikyaku>();
    var handler = new AcceptRideHandler(db, mediator);

    var act = () => handler.Handle(new AcceptRide { RideId = ride.Id, DriverId = Guid.NewGuid() }, CancellationToken.None);

    var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    Assert.Equal("not_ride_participant", ex.Message);
  }
}
