using Hub.Core.Ports.Secondary;
using Hub.Core.UseCases;
using NSubstitute;
using Voyager.Contracts.Ride;
using Xunit;

namespace Hub.Tests.UseCases;

public class RideEventHandlersTests
{
  private readonly IHubRelay _relay = Substitute.For<IHubRelay>();
  private readonly Guid _rideId = Guid.NewGuid();
  private readonly Guid _driverId = Guid.NewGuid();
  private readonly Guid _userId = Guid.NewGuid();

  [Fact]
  public async Task NewRideRequestedHandler_RelaysToDriver()
  {
    var handler = new NewRideRequestedHandler(_relay);

    await handler.Handle(new NewRideRequested { RideId = _rideId, DriverId = _driverId }, CancellationToken.None);

    await _relay.Received(1).SendToDriverNewRideRequest(_rideId, _driverId, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task RideAcceptedHandler_RelaysToRider()
  {
    var handler = new RideAcceptedHandler(_relay);

    await handler.Handle(new RideAccepted { RideId = _rideId }, CancellationToken.None);

    await _relay.Received(1).SendToRiderRideAccepted(_rideId, Arg.Any<CancellationToken>());
  }

  // Routed per-participant, not per-ride: a ride cancelled before the driver accepts has no
  // joinable ride group, so a ride-group broadcast reached nobody.
  [Fact]
  public async Task RideCancelledHandler_RelaysToBothParticipants()
  {
    var handler = new RideCancelledHandler(_relay);

    await handler.Handle(new RideCancelled { RideId = _rideId, DriverId = _driverId, UserId = _userId }, CancellationToken.None);

    await _relay.Received(1).SendToDriverRideCancel(_rideId, _driverId, _userId, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task RideCompletedHandler_RelaysToRider()
  {
    var handler = new RideCompletedHandler(_relay);

    await handler.Handle(new RideCompleted { RideId = _rideId }, CancellationToken.None);

    await _relay.Received(1).SendToRiderRideCompleted(_rideId, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task DriverRatingReceivedHandler_RelaysToDriver()
  {
    var handler = new DriverRatingReceivedHandler(_relay);

    await handler.Handle(new DriverRatingReceived { RideId = _rideId, Rating = 4 }, CancellationToken.None);

    await _relay.Received(1).SendToDriverNewRateReceived(_rideId, 4, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task RiderRatingReceivedHandler_RelaysToRider()
  {
    var handler = new RiderRatingReceivedHandler(_relay);

    await handler.Handle(new RiderRatingReceived { RideId = _rideId, Rating = 5 }, CancellationToken.None);

    await _relay.Received(1).SendToRiderNewRateReceived(_rideId, 5, Arg.Any<CancellationToken>());
  }
}
