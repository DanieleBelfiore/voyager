using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Ride.Api.Features.GetActiveRide;
using Ride.Api.Persistence;
using Xunit;
using RideEntity = Ride.Api.Entities.Ride;

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

    Assert.NotNull(result);
    Assert.Equal(ride.Id, result!.Id);
    Assert.Equal(userId, result.UserId);
  }

  [Fact]
  public async Task GetActiveRide_ShouldReturnNull_WhenNoneFound()
  {
    await using var db = new RideDbContext(NewOptions());
    var handler = new GetActiveRideHandler(db);

    var result = await handler.Handle(new GetActiveRide { UserId = Guid.NewGuid() }, CancellationToken.None);

    Assert.Null(result);
  }
}
