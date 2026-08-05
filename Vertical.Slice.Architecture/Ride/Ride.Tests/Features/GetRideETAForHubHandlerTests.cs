using Hikyaku;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using NSubstitute;
using Ride.Api.Features.GetRideETAForHub;
using Ride.Api.Persistence;
using Ride.Api.Shared;
using SharedGetRideETA = Voyager.Contracts.Ride.GetRideETA;
using Voyager.Contracts.Driver;
using Xunit;
using RideEntity = Ride.Api.Entities.Ride;

namespace Ride.Tests.Features;

public class GetRideETAForHubHandlerTests
{
  private readonly IHikyaku _mediator = Substitute.For<IHikyaku>();
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
    var handler = new GetRideETAForHubHandler(db, _mediator, _config);

    // Act
    var result = await handler.Handle(new SharedGetRideETA { Id = ride.Id, CallerId = ride.DriverId }, CancellationToken.None);

    // Assert
    Assert.NotNull(result.DistanceKm);
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
    var handler = new GetRideETAForHubHandler(db, _mediator, _config);

    // Act
    var result = await handler.Handle(new SharedGetRideETA { Id = ride.Id, CallerId = ride.DriverId }, CancellationToken.None);

    // Assert
    Assert.Null(result.DistanceKm);
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    await using var db = NewContext();
    var handler = new GetRideETAForHubHandler(db, _mediator, _config);
    var act = () => handler.Handle(new SharedGetRideETA { Id = Guid.NewGuid() }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<KeyNotFoundException>(act);
    Assert.Equal("ride_not_found", ex.Message);
  }

  /// <summary>
  /// Reachable only over the broker, which is exactly why it needs the check: without it any
  /// caller that can put a message on the bus could ask for any ride by id and learn where that
  /// driver is. Same rule as the local GET /rides/{id}/eta twin — participants only.
  /// </summary>
  [Fact]
  public async Task Handle_Throws_WhenCallerIsNotARideParticipant()
  {
    // Arrange
    await using var db = NewContext();
    var pickup = new Point(0, 0);
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), pickup, pickup);
    db.Rides.Add(ride);
    await db.SaveChangesAsync();
    var handler = new GetRideETAForHubHandler(db, _mediator, _config);

    // Act & Assert
    await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
      handler.Handle(new SharedGetRideETA { Id = ride.Id, CallerId = Guid.NewGuid() }, CancellationToken.None));

    await _mediator.DidNotReceive().Send(Arg.Any<GetDriverLocation>(), Arg.Any<CancellationToken>());
  }
}
