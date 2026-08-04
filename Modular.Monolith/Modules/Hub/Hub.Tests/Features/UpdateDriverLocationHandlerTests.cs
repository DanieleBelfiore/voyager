using Hub.Module.Features.UpdateDriverLocation;
using Hub.Module.Shared;
using Hikyaku;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.Geometries;
using NSubstitute;
using Voyager.Contracts.Driver;
using Voyager.Contracts.Ride;
using Xunit;

namespace Hub.Tests.Features;

public class UpdateDriverLocationHandlerTests
{
  private static readonly IConfiguration TestConfiguration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?> { ["Hub:ArrivalThresholdMeters"] = "500" })
    .Build();

  private static (IHikyaku Mediator, IHubContext<VoyagerHub, IVoyagerShareClient> Hub, IVoyagerShareClient GroupClient) NewMocks()
  {
    var mediator = Substitute.For<IHikyaku>();
    var hub = Substitute.For<IHubContext<VoyagerHub, IVoyagerShareClient>>();
    var clients = Substitute.For<IHubClients<IVoyagerShareClient>>();
    var groupClient = Substitute.For<IVoyagerShareClient>();

    hub.Clients.Returns(clients);
    clients.Group(Arg.Any<string>()).Returns(groupClient);

    return (mediator, hub, groupClient);
  }

  [Fact]
  public async Task UpdateDriverLocation_ShouldUpdateLocationAndPushToRider_WhenActiveRideExists()
  {
    var (mediator, hub, groupClient) = NewMocks();

    var driverId = Guid.NewGuid();
    var rideId = Guid.NewGuid();
    var newLocation = new Point(10, 10);
    var pickup = new Point(10, 10); // same point => distance 0 => arrival notification fires

    mediator.Send(Arg.Any<GetActiveRide>(), Arg.Any<CancellationToken>())
      .Returns(new ActiveRideInfo { Id = rideId, PickupLocation = pickup });
    mediator.Send(Arg.Any<GetRideETA>(), Arg.Any<CancellationToken>())
      .Returns(new RideETAInfo { EstimatedArrivalMinutes = 5, DistanceKm = 1.2 });

    var handler = new UpdateDriverLocationHandler(mediator, hub, TestConfiguration);

    await handler.Handle(new UpdateDriverLocation { DriverId = driverId, Location = newLocation }, CancellationToken.None);

    await mediator.Received(1).Send(Arg.Is<UpdateLocation>(c => c.Id == driverId), Arg.Any<CancellationToken>());
    await groupClient.Received(1).SendToRiderNewDriverLocation(newLocation);
    await groupClient.Received(1).SendToRiderNewETA(5, 1.2);
    await mediator.Received(1).Send(
      Arg.Is<TrackRideLocation>(c => c.RideId == rideId && c.Location == newLocation), Arg.Any<CancellationToken>());
    await groupClient.Received(1).SendToRiderDriverArrival(rideId);
  }

  [Fact]
  public async Task UpdateDriverLocation_ShouldOnlyUpdateLocation_WhenNoActiveRide()
  {
    var (mediator, hub, groupClient) = NewMocks();

    mediator.Send(Arg.Any<GetActiveRide>(), Arg.Any<CancellationToken>()).Returns((ActiveRideInfo)null!);

    var handler = new UpdateDriverLocationHandler(mediator, hub, TestConfiguration);

    await handler.Handle(new UpdateDriverLocation { DriverId = Guid.NewGuid(), Location = new Point(0, 0) }, CancellationToken.None);

    await groupClient.DidNotReceive().SendToRiderNewDriverLocation(Arg.Any<Point>());
  }

  // Arrival is a "still on my way" signal. Start overwrites PickupLocation with the driver's own
  // position, so once the trip is under way this distance is how far they have driven — which
  // stayed under the threshold for the first few hundred metres and re-fired the push mid-trip.
  [Fact]
  public async Task UpdateDriverLocation_ShouldNotAnnounceArrival_OnceTheTripHasStarted()
  {
    var (mediator, hub, groupClient) = NewMocks();

    var driverId = Guid.NewGuid();
    var rideId = Guid.NewGuid();
    var newLocation = new Point(10, 10);

    mediator.Send(Arg.Any<GetActiveRide>(), Arg.Any<CancellationToken>())
      .Returns(new ActiveRideInfo { Id = rideId, PickupLocation = new Point(10, 10), HasStarted = true });
    mediator.Send(Arg.Any<GetRideETA>(), Arg.Any<CancellationToken>())
      .Returns(new RideETAInfo { EstimatedArrivalMinutes = 5, DistanceKm = 1.2 });

    var handler = new UpdateDriverLocationHandler(mediator, hub, TestConfiguration);

    await handler.Handle(new UpdateDriverLocation { DriverId = driverId, Location = newLocation }, CancellationToken.None);

    await groupClient.DidNotReceive().SendToRiderDriverArrival(Arg.Any<Guid>());
    // the rest of the pipeline still runs
    await mediator.Received(1).Send(Arg.Is<TrackRideLocation>(c => c.RideId == rideId), Arg.Any<CancellationToken>());
    await groupClient.Received(1).SendToRiderNewETA(5, 1.2);
  }

}
