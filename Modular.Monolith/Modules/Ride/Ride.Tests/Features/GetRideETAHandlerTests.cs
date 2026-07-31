using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using NSubstitute;
using Ride.Module.Features.GetRideETA;
using Ride.Module.Persistence;
using Ride.Module.Shared;
using Voyager.Contracts.Driver;
using Xunit;
using RideEntity = Ride.Module.Entities.Ride;

namespace Ride.Tests.Features;

public class GetRideETAHandlerTests
{
  private readonly IMediator _mediator = Substitute.For<IMediator>();
  private readonly IOptions<EtaConfig> _config = Options.Create(new EtaConfig { AverageSpeedKmh = 30.0 });

  private static RideDbContext NewContext()
  {
    var options = new DbContextOptionsBuilder<RideDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    return new RideDbContext(options);
  }

  [Fact]
  public async Task Handle_ReturnsEta_WhenDriverLocationKnown()
  {
    // Arrange
    await using var db = NewContext();
    var pickup = new Point(0, 0);
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), pickup, pickup);
    db.Rides.Add(ride);
    await db.SaveChangesAsync();
    _mediator.Send(Arg.Any<GetDriverLocation>(), Arg.Any<CancellationToken>())
      .Returns(new DriverLocationInfo { LastLocation = new Point(0, 1) });
    var handler = new GetRideETAHandler(db, _mediator, _config);

    // Act
    var result = await handler.Handle(new GetRideETA { Id = ride.Id, CallerId = ride.UserId }, CancellationToken.None);

    // Assert: pickup and driver location are 1 degree of latitude apart (~111.2km great-circle).
    // At 30km/h that's ~222 base minutes before the 0.8x-1.6x time-of-day multiplier — asserting
    // real ranges (not just NotNull) is what would have caught the regression where this returned
    // DateTime.Now.Minute (a value in [0, 59] unrelated to distance/speed) instead.
    Assert.InRange(result.DistanceKm!.Value, 111.0, 111.3);
    Assert.InRange(result.EstimatedArrivalMinutes!.Value, 170, 360);
  }

  [Fact]
  public async Task Handle_ReturnsEmptyResponse_WhenDriverLocationUnknown()
  {
    // Arrange
    await using var db = NewContext();
    var pickup = new Point(0, 0);
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), pickup, pickup);
    db.Rides.Add(ride);
    await db.SaveChangesAsync();
    _mediator.Send(Arg.Any<GetDriverLocation>(), Arg.Any<CancellationToken>())
      .Returns((DriverLocationInfo?)null);
    var handler = new GetRideETAHandler(db, _mediator, _config);

    // Act
    var result = await handler.Handle(new GetRideETA { Id = ride.Id, CallerId = ride.UserId }, CancellationToken.None);

    // Assert
    Assert.Null(result.DistanceKm);
    Assert.Null(result.EstimatedArrivalMinutes);
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    await using var db = NewContext();
    var handler = new GetRideETAHandler(db, _mediator, _config);
    var act = () => handler.Handle(new GetRideETA { Id = Guid.NewGuid() }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("ride_not_found", ex.Message);
  }
}
