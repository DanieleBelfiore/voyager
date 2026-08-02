using System.Threading;
using System.Threading.Tasks;
using Hub.Api.Shared;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Voyager.Contracts.Ride;

namespace Hub.Api.Features.RideEvents;

/// <summary>
/// Consumes ride lifecycle events Ride publishes (Voyager.Contracts/Ride/RideEvents.cs) and
/// relays them to connected SignalR clients — no local endpoint, reachable only via Arbitrer's
/// remote notification fan-out, same rationale as Ride's *ForHub handlers.
/// </summary>
public class NewRideRequestedHandler(IHubContext<VoyagerHub, IVoyagerShareClient> hub) : INotificationHandler<NewRideRequested>
{
  // Targets the driver's personal group, not the ride group: at Requested status nobody has
  // called JoinRideGroup yet (there's no active ride to authorize it against), so ride_{rideId}
  // would have zero members and the notification would be silently dropped.
  public Task Handle(NewRideRequested notification, CancellationToken cancellationToken) =>
    hub.Clients.Group(HubGroups.ForUser(notification.DriverId)).SendToDriverNewRideRequest(notification.RideId);
}

public class RideAcceptedHandler(IHubContext<VoyagerHub, IVoyagerShareClient> hub) : INotificationHandler<RideAccepted>
{
  public Task Handle(RideAccepted notification, CancellationToken cancellationToken) =>
    hub.Clients.Group(HubGroups.ForRide(notification.RideId)).SendToRiderRideAccepted(notification.RideId);
}

public class RideCancelledHandler(IHubContext<VoyagerHub, IVoyagerShareClient> hub) : INotificationHandler<RideCancelled>
{
  // Personal groups, not ride_{RideId}: a ride cancelled at Requested status was never joinable
  // (JoinRideGroup authorizes against an *active* ride), so the ride group is empty and the
  // driver — still driving to a pickup that no longer exists — was never told. Same reasoning as
  // NewRideRequestedHandler above. Two distinct user groups, so nobody is notified twice.
  public Task Handle(RideCancelled notification, CancellationToken cancellationToken) =>
    hub.Clients.Groups(HubGroups.ForUser(notification.DriverId), HubGroups.ForUser(notification.UserId))
      .SendToDriverRideCancel(notification.RideId);
}

public class RideCompletedHandler(IHubContext<VoyagerHub, IVoyagerShareClient> hub) : INotificationHandler<RideCompleted>
{
  public Task Handle(RideCompleted notification, CancellationToken cancellationToken) =>
    hub.Clients.Group(HubGroups.ForRide(notification.RideId)).SendToRiderRideCompleted(notification.RideId);
}

public class DriverRatingReceivedHandler(IHubContext<VoyagerHub, IVoyagerShareClient> hub) : INotificationHandler<DriverRatingReceived>
{
  public Task Handle(DriverRatingReceived notification, CancellationToken cancellationToken) =>
    hub.Clients.Group(HubGroups.ForRide(notification.RideId)).SendToDriverNewRateReceived(notification.Rating);
}

public class RiderRatingReceivedHandler(IHubContext<VoyagerHub, IVoyagerShareClient> hub) : INotificationHandler<RiderRatingReceived>
{
  public Task Handle(RiderRatingReceived notification, CancellationToken cancellationToken) =>
    hub.Clients.Group(HubGroups.ForRide(notification.RideId)).SendToRiderNewRateReceived(notification.Rating);
}
