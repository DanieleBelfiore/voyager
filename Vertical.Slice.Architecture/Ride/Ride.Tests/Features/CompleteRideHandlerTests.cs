using Hikyaku;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using NSubstitute;
using Ride.Api.Features.CompleteRide;
using Ride.Api.Persistence;
using Ride.Api.Shared;
using Voyager.Contracts.Driver;
using Voyager.Contracts.Ride;
using Xunit;
using RideEntity = Ride.Api.Entities.Ride;
using RideStatus = Ride.Api.Entities.RideStatus;

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
    var driverId = Guid.NewGuid();
    var ride = new RideEntity(Guid.NewGuid(), driverId, SomePoint, new Point(0, 1));
    ride.Accept(driverId);
    ride.Start(SomePoint);
    db.Rides.Add(ride);
    await db.SaveChangesAsync();
    var mediator = Substitute.For<IHikyaku>();
    var handler = new CompleteRideHandler(db, mediator, FareConfig);
    var dropoff = new Point(0, 1);

    // Act
    await handler.Handle(new CompleteRide { Id = ride.Id, Location = dropoff, CallerId = driverId }, CancellationToken.None);

    // Assert: server computes price from the agreed route (pickup (0,0) → dropoff (0,1),
    // ~111.2km) and elapsed time — never from a client-supplied value. Start() stamped StartAt moments ago,
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
    var handler = new CompleteRideHandler(db, Substitute.For<IHikyaku>(), FareConfig);
    var act = () => handler.Handle(new CompleteRide { Id = Guid.NewGuid(), Location = SomePoint }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<KeyNotFoundException>(act);
    Assert.Equal("no_ride_found", ex.Message);
  }

  /// <summary>
  /// The completion coordinate is supplied by the driver, who is the party being paid. Pricing it
  /// let them name a point far past the agreed destination and charge for the difference, and
  /// overwriting DropoffLocation with it destroyed the evidence — the ride then read as though
  /// the rider had asked to go there. The fare comes off the route the rider agreed to; where the
  /// driver actually stopped is recorded on LastLocation, which is not priced.
  /// </summary>
  [Fact]
  public async Task Handle_PricesTheAgreedRoute_WhenTheDriverClaimsAFartherDropoff()
  {
    await using var db = NewContext();
    var pickup = new Point(0, 0);
    var agreedDropoff = new Point(0, 1);
    var claimed = new Point(0, 5);
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), pickup, agreedDropoff);
    ride.Accept(ride.DriverId);
    ride.Start(pickup);
    db.Rides.Add(ride);
    await db.SaveChangesAsync();
    var handler = new CompleteRideHandler(db, Substitute.For<IHikyaku>(), FareConfig);

    await handler.Handle(new CompleteRide { Id = ride.Id, Location = claimed, CallerId = ride.DriverId }, CancellationToken.None);

    var expectedPrice = 2.5 + 1.2 * (RideEtaCalculator.DistanceInMeters(pickup, agreedDropoff) / 1000);
    Assert.InRange(ride.Price!.Value, expectedPrice - 0.1, expectedPrice + 0.1);
    Assert.Equal(agreedDropoff, ride.DropoffLocation);
    Assert.Equal(claimed, ride.LastLocation);
  }

  /// <summary>
  /// The other end of the same hole: Start used to overwrite PickupLocation with wherever the
  /// driver said they were, so a driver could stretch the priced segment from both ends at once.
  /// </summary>
  [Fact]
  public async Task Handle_PricesTheAgreedRoute_WhenTheDriverStartedFarFromThePickup()
  {
    await using var db = NewContext();
    var pickup = new Point(0, 0);
    var agreedDropoff = new Point(0, 1);
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), pickup, agreedDropoff);
    ride.Accept(ride.DriverId);
    ride.Start(new Point(0, -4));
    db.Rides.Add(ride);
    await db.SaveChangesAsync();
    var handler = new CompleteRideHandler(db, Substitute.For<IHikyaku>(), FareConfig);

    await handler.Handle(new CompleteRide { Id = ride.Id, Location = agreedDropoff, CallerId = ride.DriverId }, CancellationToken.None);

    var expectedPrice = 2.5 + 1.2 * (RideEtaCalculator.DistanceInMeters(pickup, agreedDropoff) / 1000);
    Assert.InRange(ride.Price!.Value, expectedPrice - 0.1, expectedPrice + 0.1);
    Assert.Equal(pickup, ride.PickupLocation);
  }
}
