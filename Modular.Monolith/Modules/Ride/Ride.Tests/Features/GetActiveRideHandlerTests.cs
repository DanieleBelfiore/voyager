using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Ride.Module.Features.GetActiveRide;
using Ride.Module.Persistence;
using Xunit;
using RideEntity = Ride.Module.Entities.Ride;

namespace Ride.Tests.Features;

public class GetActiveRideHandlerTests
{
  private static DbContextOptions<RideDbContext> NewOptions() => new DbContextOptionsBuilder<RideDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString())
    .Options;

  [Fact]
  public async Task GetActiveRide_ShouldReturnRide_WhenFound()
  {
    await using var db = new RideDbContext(NewOptions());
    var userId = Guid.NewGuid();
    var ride = new RideEntity(userId, Guid.NewGuid(), new Point(0, 0), new Point(1, 1));
    ride.Accept(ride.DriverId);
    db.Rides.Add(ride);
    await db.SaveChangesAsync();

    var handler = new GetActiveRideHandler(db);

    var result = await handler.Handle(new GetActiveRide { UserId = userId }, CancellationToken.None);

    result.Should().NotBeNull();
    result!.Id.Should().Be(ride.Id);
    result.UserId.Should().Be(userId);
  }

  [Fact]
  public async Task GetActiveRide_ShouldReturnNull_WhenNoneFound()
  {
    await using var db = new RideDbContext(NewOptions());
    var handler = new GetActiveRideHandler(db);

    var result = await handler.Handle(new GetActiveRide { UserId = Guid.NewGuid() }, CancellationToken.None);

    result.Should().BeNull();
  }
}
