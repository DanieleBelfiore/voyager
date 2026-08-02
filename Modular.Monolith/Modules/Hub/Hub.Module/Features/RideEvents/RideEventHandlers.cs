using System.Threading;
using System.Threading.Tasks;
using Hub.Module.Shared;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Voyager.Contracts.Ride;

namespace Hub.Module.Features.RideEvents;

/// <summary>
/// Consumes ride lifecycle events Ride's module publishes (Voyager.Contracts/Ride/RideEvents.cs)
/// via IMediator.Publish and relays them to connected SignalR clients — no controller, MediatR's
/// own in-process notification fan-out is what wires Ride's Publish call to these handlers.
/// </summary>
internal class NewRideRequestedHandler(IHubContext<VoyagerHub, IVoyagerShareClient> hub) : INotificationHandler<NewRideRequested>
{
  // Targets the driver's personal group, not the ride group: at Requested status nobody has
  // called JoinRideGroup yet (there's no active ride to authorize it against), so ride_{rideId}
  // would have zero members and the notification would be silently dropped.
  public Task Handle(NewRideRequested notification, CancellationToken cancellationToken) =>
    hub.Clients.Group(HubGroups.ForUser(notification.DriverId)).SendToDriverNewRideRequest(notification.RideId);
}

internal class RideAcceptedHandler(IHubContext<VoyagerHub, IVoyagerShareClient> hub) : INotificationHandler<RideAccepted>
{
  public Task Handle(RideAccepted notification, CancellationToken cancellationToken) =>
    hub.Clients.Group(HubGroups.ForRide(notification.RideId)).SendToRiderRideAccepted(notification.RideId);
}

internal class RideCancelledHandler(IHubContext<VoyagerHub, IVoyagerShareClient> hub) : INotificationHandler<RideCancelled>
{
  public Task Handle(RideCancelled notification, CancellationToken cancellationToken) =>
    hub.Clients.Group(HubGroups.ForRide(notification.RideId)).SendToDriverRideCancel(notification.RideId);
}

internal class RideCompletedHandler(IHubContext<VoyagerHub, IVoyagerShareClient> hub) : INotificationHandler<RideCompleted>
{
  public Task Handle(RideCompleted notification, CancellationToken cancellationToken) =>
    hub.Clients.Group(HubGroups.ForRide(notification.RideId)).SendToRiderRideCompleted(notification.RideId);
}

internal class DriverRatingReceivedHandler(IHubContext<VoyagerHub, IVoyagerShareClient> hub) : INotificationHandler<DriverRatingReceived>
{
  public Task Handle(DriverRatingReceived notification, CancellationToken cancellationToken) =>
    hub.Clients.Group(HubGroups.ForRide(notification.RideId)).SendToDriverNewRateReceived(notification.Rating);
}

internal class RiderRatingReceivedHandler(IHubContext<VoyagerHub, IVoyagerShareClient> hub) : INotificationHandler<RiderRatingReceived>
{
  public Task Handle(RiderRatingReceived notification, CancellationToken cancellationToken) =>
    hub.Clients.Group(HubGroups.ForRide(notification.RideId)).SendToRiderNewRateReceived(notification.Rating);
}
