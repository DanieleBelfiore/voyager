using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Ride.Api.Features.StartRide;
using Ride.Api.Persistence;
using Xunit;
using RideEntity = Ride.Api.Entities.Ride;
using RideStatus = Ride.Api.Entities.RideStatus;

namespace Ride.Tests.Features;

public class StartRideHandlerTests
{
  [Fact]
  public async Task StartRide_ShouldSetInProgressAndLocation()
  {
    var options = new DbContextOptionsBuilder<RideDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var db = new RideDbContext(options);
    var driverId = Guid.NewGuid();
    var ride = new RideEntity(Guid.NewGuid(), driverId, new Point(0, 0), new Point(1, 1));
    ride.Accept(driverId);
    db.Rides.Add(ride);
    await db.SaveChangesAsync();

    var handler = new StartRideHandler(db);
    var startLocation = new Point(2, 2);

    await handler.Handle(new StartRide { Id = ride.Id, Location = startLocation, CallerId = driverId }, CancellationToken.None);

    Assert.Equal(RideStatus.InProgress, ride.Status);
    Assert.Equal(startLocation, ride.PickupLocation);
    Assert.NotNull(ride.StartAt);
  }
}
