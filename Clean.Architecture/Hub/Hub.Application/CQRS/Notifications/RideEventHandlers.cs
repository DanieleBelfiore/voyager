using System.Threading;
using System.Threading.Tasks;
using Hub.Application.Ports;
using MediatR;
using Voyager.Contracts.Ride;

namespace Hub.Application.CQRS.Notifications;

/// <summary>
/// Consumes the ride lifecycle events Ride publishes (see Voyager.Contracts/Ride/RideEvents.cs)
/// and relays them to connected SignalR clients. Hub is the service that actually owns those
/// connections, so this is where the Plugin.Microservices.CQRS variant's dead cross-process
/// IHubContext calls become real.
/// </summary>
public class NewRideRequestedHandler(IHubRelay relay) : INotificationHandler<NewRideRequested>
{
  public Task Handle(NewRideRequested notification, CancellationToken cancellationToken) =>
    relay.SendToDriverNewRideRequest(notification.RideId, notification.DriverId, cancellationToken);
}

public class RideAcceptedHandler(IHubRelay relay) : INotificationHandler<RideAccepted>
{
  public Task Handle(RideAccepted notification, CancellationToken cancellationToken) =>
    relay.SendToRiderRideAccepted(notification.RideId, cancellationToken);
}

public class RideCancelledHandler(IHubRelay relay) : INotificationHandler<RideCancelled>
{
  public Task Handle(RideCancelled notification, CancellationToken cancellationToken) =>
    relay.SendToDriverRideCancel(notification.RideId, notification.DriverId, notification.UserId, cancellationToken);
}

public class RideCompletedHandler(IHubRelay relay) : INotificationHandler<RideCompleted>
{
  public Task Handle(RideCompleted notification, CancellationToken cancellationToken) =>
    relay.SendToRiderRideCompleted(notification.RideId, cancellationToken);
}

public class DriverRatingReceivedHandler(IHubRelay relay) : INotificationHandler<DriverRatingReceived>
{
  public Task Handle(DriverRatingReceived notification, CancellationToken cancellationToken) =>
    relay.SendToDriverNewRateReceived(notification.RideId, notification.Rating, cancellationToken);
}

public class RiderRatingReceivedHandler(IHubRelay relay) : INotificationHandler<RiderRatingReceived>
{
  public Task Handle(RiderRatingReceived notification, CancellationToken cancellationToken) =>
    relay.SendToRiderNewRateReceived(notification.RideId, notification.Rating, cancellationToken);
}
