using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NSubstitute;
using Ride.Api.Features.RateDriver;
using Ride.Api.Persistence;
using Voyager.Contracts.Identity;
using Voyager.Contracts.Ride;
using Xunit;
using RideEntity = Ride.Api.Entities.Ride;

namespace Ride.Tests.Features;

public class RateDriverHandlerTests
{
  private static readonly Point SomePoint = new(0, 0);

  private static RideDbContext NewContext()
  {
    var options = new DbContextOptionsBuilder<RideDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    return new RideDbContext(options);
  }

  [Fact]
  public async Task Handle_UpdatesRatingAndPublishesEvent_WhenDriverHasCompletedRides()
  {
    // Arrange
    await using var db = NewContext();
    var driverId = Guid.NewGuid();
    var ride = new RideEntity(Guid.NewGuid(), driverId, SomePoint, SomePoint);
    ride.Start(SomePoint);
    ride.Complete(SomePoint, 10);
    db.Rides.Add(ride);
    await db.SaveChangesAsync();
    var mediator = Substitute.For<IMediator>();
    var handler = new RateDriverHandler(db, mediator);

    // Act
    await handler.Handle(new RateDriver { RideId = ride.Id, Rating = 5 }, CancellationToken.None);

    // Assert
    await mediator.Received(1).Send(Arg.Is<UpdateUserRating>(c => c.UserId == driverId && c.Rating == 5 && c.Rides == 1), Arg.Any<CancellationToken>());
    await mediator.Received(1).Publish(Arg.Is<DriverRatingReceived>(e => e.RideId == ride.Id && e.Rating == 5), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_DoesNotPublishEvent_WhenDriverHasNoCompletedRides()
  {
    // Arrange
    await using var db = NewContext();
    var driverId = Guid.NewGuid();
    var ride = new RideEntity(Guid.NewGuid(), driverId, SomePoint, SomePoint); // still Requested, not Completed
    db.Rides.Add(ride);
    await db.SaveChangesAsync();
    var mediator = Substitute.For<IMediator>();
    var handler = new RateDriverHandler(db, mediator);

    // Act
    await handler.Handle(new RateDriver { RideId = ride.Id, Rating = 5 }, CancellationToken.None);

    // Assert
    await mediator.DidNotReceive().Publish(Arg.Any<DriverRatingReceived>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    await using var db = NewContext();
    var handler = new RateDriverHandler(db, Substitute.For<IMediator>());
    var act = () => handler.Handle(new RateDriver { RideId = Guid.NewGuid(), Rating = 3 }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("ride_not_found", ex.Message);
  }
}
