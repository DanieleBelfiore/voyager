using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NSubstitute;
using Ride.Api.Features.CancelRide;
using Ride.Api.Persistence;
using Voyager.Contracts.Driver;
using Voyager.Contracts.Ride;
using Voyager.Errors;
using Xunit;
using RideEntity = Ride.Api.Entities.Ride;
using RideStatus = Ride.Api.Entities.RideStatus;

namespace Ride.Tests.Features;

public class CancelRideHandlerTests
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
  public async Task Handle_CancelsRideAndPublishesEvent_WhenCancellable()
  {
    // Arrange
    await using var db = NewContext();
    var userId = Guid.NewGuid();
    var ride = new RideEntity(userId, Guid.NewGuid(), SomePoint, SomePoint);
    db.Rides.Add(ride);
    await db.SaveChangesAsync();
    var mediator = Substitute.For<IMediator>();
    var handler = new CancelRideHandler(db, mediator);

    // Act
    await handler.Handle(new CancelRide { Id = ride.Id, CancellationReason = "changed_mind", CallerId = userId }, CancellationToken.None);

    // Assert
    Assert.Equal(RideStatus.Cancelled, ride.Status);
    Assert.Equal("changed_mind", ride.CancellationReason);
    await mediator.Received(1).Publish(Arg.Is<RideCancelled>(e => e.RideId == ride.Id), Arg.Any<CancellationToken>());
    await mediator.Received(1).Send(Arg.Is<MarkDriverAvailable>(c => c.DriverId == ride.DriverId), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    await using var db = NewContext();
    var handler = new CancelRideHandler(db, Substitute.For<IMediator>());
    var act = () => handler.Handle(new CancelRide { Id = Guid.NewGuid(), CancellationReason = "x" }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<KeyNotFoundException>(act);
    Assert.Equal("no_ride_found", ex.Message);
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotCancellable()
  {
    // Arrange
    await using var db = NewContext();
    var driverId = Guid.NewGuid();
    var ride = new RideEntity(Guid.NewGuid(), driverId, SomePoint, SomePoint);
    ride.Accept(driverId);
    ride.Start(SomePoint);
    db.Rides.Add(ride);
    await db.SaveChangesAsync();
    var handler = new CancelRideHandler(db, Substitute.For<IMediator>());
    var act = () => handler.Handle(new CancelRide { Id = ride.Id, CancellationReason = "x", CallerId = driverId }, CancellationToken.None);

    // Act & Assert
    await Assert.ThrowsAsync<ConflictException>(act);
  }
}
