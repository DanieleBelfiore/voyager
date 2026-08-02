using Hub.API;
using Hub.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using Ride.Core.CQRS.Events;
using Xunit;

namespace Hub.Tests;

public class RideEventHandlersTests
{
  private readonly IHubContext<VoyagerHub, IVoyagerShareClient> _hub;
  private readonly IVoyagerShareClient _groupClient;
  private readonly Guid _rideId = Guid.NewGuid();
  private readonly Guid _driverId = Guid.NewGuid();
  private readonly Guid _userId = Guid.NewGuid();
  private readonly IHubClients<IVoyagerShareClient> _clients;

  public RideEventHandlersTests()
  {
    _hub = Substitute.For<IHubContext<VoyagerHub, IVoyagerShareClient>>();
    _clients = Substitute.For<IHubClients<IVoyagerShareClient>>();
    _groupClient = Substitute.For<IVoyagerShareClient>();
    _hub.Clients.Returns(_clients);
    _clients.Group($"ride_{_rideId}").Returns(_groupClient);
    _clients.Group($"user_{_driverId}").Returns(_groupClient);
    _clients.Groups(Arg.Any<IReadOnlyList<string>>()).Returns(_groupClient);
  }

  [Fact]
  public async Task NewRideRequestedHandler_RelaysToDriversPersonalGroup()
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

  // Routed to both participants' personal groups, never ride_{RideId} — a ride cancelled before
  // the driver accepts has no joinable ride group, so that broadcast reached nobody.
  [Fact]
  public async Task RideCancelledHandler_RelaysToBothParticipantsPersonalGroups()
  {
    var handler = new RideCancelledHandler(_hub);

    await handler.Handle(new RideCancelled { RideId = _rideId, DriverId = _driverId, UserId = _userId }, CancellationToken.None);

    _clients.Received(1).Groups(Arg.Is<IReadOnlyList<string>>(g =>
      g.Contains($"user_{_driverId}") && g.Contains($"user_{_userId}")));
    await _groupClient.Received(1).SendToDriverRideCancel(_rideId);
  }

  [Fact]
  public async Task RideCancelledHandler_DoesNotUseTheRideGroup()
  {
    var handler = new RideCancelledHandler(_hub);

    await handler.Handle(new RideCancelled { RideId = _rideId, DriverId = _driverId, UserId = _userId }, CancellationToken.None);

    _clients.DidNotReceive().Group($"ride_{_rideId}");
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

    await handler.Handle(new DriverRatingReceived { RideId = _rideId, Rating = 5 }, CancellationToken.None);

    await _groupClient.Received(1).SendToDriverNewRateReceived(5);
  }

  [Fact]
  public async Task RiderRatingReceivedHandler_RelaysToRider()
  {
    var handler = new RiderRatingReceivedHandler(_hub);

    await handler.Handle(new RiderRatingReceived { RideId = _rideId, Rating = 4 }, CancellationToken.None);

    await _groupClient.Received(1).SendToRiderNewRateReceived(4);
  }
}
