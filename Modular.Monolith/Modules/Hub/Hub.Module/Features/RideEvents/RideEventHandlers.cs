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
  public Task Handle(NewRideRequested notification, CancellationToken cancellationToken) =>
    hub.Clients.Group(HubGroups.ForRide(notification.RideId)).SendToDriverNewRideRequest(notification.RideId);
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
