using System.Threading;
using System.Threading.Tasks;
using Hub.Core.Ports.Secondary;
using MediatR;
using Voyager.Contracts.Ride;

namespace Hub.Core.UseCases;

/// <summary>
/// Consumes ride lifecycle events Ride publishes (Voyager.Contracts/Ride/RideEvents.cs) and
/// relays them to connected SignalR clients. No primary port — reachable only via Arbitrer's
/// remote notification fan-out, same rationale as Ride's *ForHub use cases.
/// </summary>
public class NewRideRequestedHandler(IHubRelay relay) : INotificationHandler<NewRideRequested>
{
  public Task Handle(NewRideRequested notification, CancellationToken cancellationToken) =>
    relay.SendToDriverNewRideRequest(notification.RideId, cancellationToken);
}

public class RideAcceptedHandler(IHubRelay relay) : INotificationHandler<RideAccepted>
{
  public Task Handle(RideAccepted notification, CancellationToken cancellationToken) =>
    relay.SendToRiderRideAccepted(notification.RideId, cancellationToken);
}

public class RideCancelledHandler(IHubRelay relay) : INotificationHandler<RideCancelled>
{
  public Task Handle(RideCancelled notification, CancellationToken cancellationToken) =>
    relay.SendToDriverRideCancel(notification.RideId, cancellationToken);
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
