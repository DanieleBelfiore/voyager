using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NSubstitute;
using Ride.Api.Features.AcceptRide;
using Ride.Api.Persistence;
using Voyager.Contracts.Ride;
using Xunit;
using RideEntity = Ride.Api.Entities.Ride;
using RideStatus = Ride.Api.Entities.RideStatus;

namespace Ride.Tests.Features;

public class AcceptRideHandlerTests
{
  private static readonly Point SomePoint = new(0, 0);

  [Fact]
  public async Task AcceptRide_ShouldAssignDriverAndPersist()
  {
    var options = new DbContextOptionsBuilder<RideDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var db = new RideDbContext(options);
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), SomePoint, SomePoint);
    db.Rides.Add(ride);
    await db.SaveChangesAsync();

    var mediator = Substitute.For<IMediator>();
    var handler = new AcceptRideHandler(db, mediator);
    var driverId = Guid.NewGuid();

    await handler.Handle(new AcceptRide { RideId = ride.Id, DriverId = driverId }, CancellationToken.None);

    ride.DriverId.Should().Be(driverId);
    ride.Status.Should().Be(RideStatus.DriverAssigned);
    await mediator.Received(1).Publish(Arg.Is<RideAccepted>(e => e.RideId == ride.Id), Arg.Any<CancellationToken>());
  }
}
