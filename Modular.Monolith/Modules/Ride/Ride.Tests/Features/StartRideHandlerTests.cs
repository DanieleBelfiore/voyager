using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Ride.Module.Features.StartRide;
using Ride.Module.Persistence;
using Xunit;
using RideEntity = Ride.Module.Entities.Ride;
using RideStatus = Ride.Module.Entities.RideStatus;

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
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), new Point(0, 0), new Point(1, 1));
    db.Rides.Add(ride);
    await db.SaveChangesAsync();

    var handler = new StartRideHandler(db);
    var startLocation = new Point(2, 2);

    await handler.Handle(new StartRide { Id = ride.Id, Location = startLocation }, CancellationToken.None);

    ride.Status.Should().Be(RideStatus.InProgress);
    ride.PickupLocation.Should().Be(startLocation);
    ride.StartAt.Should().NotBeNull();
  }
}
