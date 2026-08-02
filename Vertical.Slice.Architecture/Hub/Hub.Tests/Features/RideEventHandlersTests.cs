using Hub.Api.Features.RideEvents;
using Hub.Api.Shared;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using Voyager.Contracts.Ride;
using Xunit;

namespace Hub.Tests.Features;

public class RideEventHandlersTests
{
  private readonly IHubContext<VoyagerHub, IVoyagerShareClient> _hub;
  private readonly IVoyagerShareClient _groupClient;
  private readonly Guid _rideId = Guid.NewGuid();
  private readonly Guid _driverId = Guid.NewGuid();

  public RideEventHandlersTests()
  {
    _hub = Substitute.For<IHubContext<VoyagerHub, IVoyagerShareClient>>();
    var clients = Substitute.For<IHubClients<IVoyagerShareClient>>();
    _groupClient = Substitute.For<IVoyagerShareClient>();
    _hub.Clients.Returns(clients);
    clients.Group(HubGroups.ForRide(_rideId)).Returns(_groupClient);
    clients.Group(HubGroups.ForUser(_driverId)).Returns(_groupClient);
  }

  [Fact]
  public async Task NewRideRequestedHandler_RelaysToDriver()
  {
    var handler = new NewRideRequestedHandler(_hub);

    await handler.Handle(new NewRideRequested { RideId = _rideId, DriverId = _driverId }, CancellationToken.None);

    await _groupClient.Received(1).SendToDriverNewRideRequest(_rideId);
  }

  [Fact]
  public async Task RideAcceptedHandler_RelaysToRider()
  {
    var handler = new RideAcceptedHandler(_hub);

    await handler.Handle(new RideAccepted { RideId = _rideId }, CancellationToken.None);

    await _groupClient.Received(1).SendToRiderRideAccepted(_rideId);
  }

  [Fact]
  public async Task RideCancelledHandler_RelaysToDriver()
  {
    var handler = new RideCancelledHandler(_hub);

    await handler.Handle(new RideCancelled { RideId = _rideId }, CancellationToken.None);

    await _groupClient.Received(1).SendToDriverRideCancel(_rideId);
  }

  [Fact]
  public async Task RideCompletedHandler_RelaysToRider()
  {
    var handler = new RideCompletedHandler(_hub);

    await handler.Handle(new RideCompleted { RideId = _rideId }, CancellationToken.None);

    await _groupClient.Received(1).SendToRiderRideCompleted(_rideId);
  }

  [Fact]
  public async Task DriverRatingReceivedHandler_RelaysToDriver()
  {
    var handler = new DriverRatingReceivedHandler(_hub);

    await handler.Handle(new DriverRatingReceived { RideId = _rideId, Rating = 4 }, CancellationToken.None);

    await _groupClient.Received(1).SendToDriverNewRateReceived(4);
  }

  [Fact]
  public async Task RiderRatingReceivedHandler_RelaysToRider()
  {
    var handler = new RiderRatingReceivedHandler(_hub);

    await handler.Handle(new RiderRatingReceived { RideId = _rideId, Rating = 5 }, CancellationToken.None);

    await _groupClient.Received(1).SendToRiderNewRateReceived(5);
  }
}
