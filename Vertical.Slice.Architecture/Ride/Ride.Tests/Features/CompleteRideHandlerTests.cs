using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NSubstitute;
using Ride.Api.Features.CompleteRide;
using Ride.Api.Persistence;
using Voyager.Contracts.Ride;
using Xunit;
using RideEntity = Ride.Api.Entities.Ride;
using RideStatus = Ride.Api.Entities.RideStatus;

namespace Ride.Tests.Features;

public class CompleteRideHandlerTests
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
  public async Task Handle_CompletesRideAndPublishesEvent_WhenRideExists()
  {
    // Arrange
    await using var db = NewContext();
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), SomePoint, SomePoint);
    db.Rides.Add(ride);
    await db.SaveChangesAsync();
    var mediator = Substitute.For<IMediator>();
    var handler = new CompleteRideHandler(db, mediator);
    var dropoff = new Point(3, 3);

    // Act
    await handler.Handle(new CompleteRide { Id = ride.Id, Location = dropoff, Price = 42.5 }, CancellationToken.None);

    // Assert
    Assert.Equal(RideStatus.Completed, ride.Status);
    Assert.Equal(dropoff, ride.DropoffLocation);
    Assert.Equal(42.5, ride.Price);
    await mediator.Received(1).Publish(Arg.Is<RideCompleted>(e => e.RideId == ride.Id), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    await using var db = NewContext();
    var handler = new CompleteRideHandler(db, Substitute.For<IMediator>());
    var act = () => handler.Handle(new CompleteRide { Id = Guid.NewGuid(), Location = SomePoint, Price = 0 }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("no_ride_found", ex.Message);
  }
}
