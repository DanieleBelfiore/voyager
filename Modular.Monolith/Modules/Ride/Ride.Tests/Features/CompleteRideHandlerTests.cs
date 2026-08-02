using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using NSubstitute;
using Ride.Module.Features.CompleteRide;
using Ride.Module.Persistence;
using Ride.Module.Shared;
using Voyager.Contracts.Driver;
using Voyager.Contracts.Ride;
using Xunit;
using RideEntity = Ride.Module.Entities.Ride;
using RideStatus = Ride.Module.Entities.RideStatus;

namespace Ride.Tests.Features;

public class CompleteRideHandlerTests
{
  private static readonly Point SomePoint = new(0, 0);
  private static readonly IOptions<FareConfig> FareConfig = Options.Create(new FareConfig { BaseFare = 2.5, PerKmRate = 1.2, PerMinuteRate = 0.25 });

  private static RideDbContext NewContext()
  {
    var options = new DbContextOptionsBuilder<RideDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    return new RideDbContext(options);
  }

  [Fact]
  public async Task Handle_CompletesRideAndPublishesEvent_WhenRideExists()
  {
    // Arrange
    await using var db = NewContext();
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), SomePoint, SomePoint);
    ride.Accept(ride.DriverId);
    ride.Start(SomePoint);
    db.Rides.Add(ride);
    await db.SaveChangesAsync();
    var mediator = Substitute.For<IMediator>();
    var handler = new CompleteRideHandler(db, mediator, FareConfig);
    var dropoff = new Point(0, 1);

    // Act
    await handler.Handle(new CompleteRide { Id = ride.Id, Location = dropoff, CallerId = ride.DriverId }, CancellationToken.None);

    // Assert: server computes price from distance (pickup (0,0) → dropoff (0,1), ~111.2km) and
    // elapsed time — never trusts a client-supplied value. Start() stamped StartAt moments ago,
    // so the duration term is ~0 here and the distance term dominates. Pinning the arithmetic
    // (rather than just "above BaseFare") is what makes a dropped term, a missing /1000, or a
    // distance/duration swap actually fail this test.
    var distanceKm = RideEtaCalculator.DistanceInMeters(SomePoint, dropoff) / 1000;
    var expectedPrice = 2.5 + 1.2 * distanceKm;
    Assert.Equal(RideStatus.Completed, ride.Status);
    Assert.Equal(dropoff, ride.DropoffLocation);
    Assert.InRange(ride.Price!.Value, expectedPrice - 0.1, expectedPrice + 0.1);
    await mediator.Received(1).Publish(Arg.Is<RideCompleted>(e => e.RideId == ride.Id), Arg.Any<CancellationToken>());
    await mediator.Received(1).Send(Arg.Is<MarkDriverAvailable>(c => c.DriverId == ride.DriverId), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    await using var db = NewContext();
    var handler = new CompleteRideHandler(db, Substitute.For<IMediator>(), FareConfig);
    var act = () => handler.Handle(new CompleteRide { Id = Guid.NewGuid(), Location = SomePoint }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<KeyNotFoundException>(act);
    Assert.Equal("no_ride_found", ex.Message);
  }
}
