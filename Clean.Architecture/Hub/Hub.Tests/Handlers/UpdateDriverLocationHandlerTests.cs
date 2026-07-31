using NetTopologySuite.Geometries;
using Hub.Application.CQRS.Commands;
using Hub.Application.Ports;
using NSubstitute;
using Xunit;

namespace Hub.Tests.Handlers;

public class UpdateDriverLocationHandlerTests
{
  private sealed class TestHubConfig : IHubConfig
  {
    public double ArrivalThresholdMeters { get; init; } = 500;
  }

  [Fact]
  public async Task UpdateDriverLocation_ShouldUpdateLocationAndPushToRider_WhenActiveRideExists()
  {
    var locationUpdater = Substitute.For<IDriverLocationUpdater>();
    var activeRideQuery = Substitute.For<IActiveRideQuery>();
    var etaQuery = Substitute.For<IRideEtaQuery>();
    var relay = Substitute.For<IHubRelay>();

    var driverId = Guid.NewGuid();
    var rideId = Guid.NewGuid();
    var newLocation = new Point(10, 10);
    var pickup = new Point(10, 10); // same point => distance 0 => arrival notification fires

    activeRideQuery.GetActiveRideForDriverAsync(driverId, Arg.Any<CancellationToken>())
      .Returns(new ActiveRide { Id = rideId, PickupLocation = pickup });
    etaQuery.GetEtaAsync(rideId, Arg.Any<CancellationToken>())
      .Returns(new RideEta { EstimatedArrivalMinutes = 5, DistanceKm = 1.2 });

    var handler = new UpdateDriverLocationHandler(locationUpdater, activeRideQuery, etaQuery, relay, new TestHubConfig());

    await handler.Handle(new UpdateDriverLocation { DriverId = driverId, Location = newLocation }, CancellationToken.None);

    await locationUpdater.Received(1).UpdateLocationAsync(driverId, newLocation, Arg.Any<CancellationToken>());
    await relay.Received(1).SendToRiderNewDriverLocation(rideId, newLocation, Arg.Any<CancellationToken>());
    await relay.Received(1).SendToRiderNewETA(rideId, 5, 1.2, Arg.Any<CancellationToken>());
    await relay.Received(1).SendToRiderDriverArrival(rideId, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task UpdateDriverLocation_ShouldOnlyUpdateLocation_WhenNoActiveRide()
  {
    var locationUpdater = Substitute.For<IDriverLocationUpdater>();
    var activeRideQuery = Substitute.For<IActiveRideQuery>();
    var etaQuery = Substitute.For<IRideEtaQuery>();
    var relay = Substitute.For<IHubRelay>();

    activeRideQuery.GetActiveRideForDriverAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ActiveRide?)null);

    var handler = new UpdateDriverLocationHandler(locationUpdater, activeRideQuery, etaQuery, relay, new TestHubConfig());

    await handler.Handle(new UpdateDriverLocation { DriverId = Guid.NewGuid(), Location = new Point(0, 0) }, CancellationToken.None);

    await relay.DidNotReceive().SendToRiderNewDriverLocation(Arg.Any<Guid>(), Arg.Any<Point>(), Arg.Any<CancellationToken>());
  }
}
