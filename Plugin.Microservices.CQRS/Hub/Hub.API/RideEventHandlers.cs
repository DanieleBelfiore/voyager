using System.Threading;
using System.Threading.Tasks;
using Hub.Core.Interfaces;
using Hikyaku;
using Microsoft.AspNetCore.SignalR;
using Ride.Core.CQRS.Events;

namespace Hub.API;

/// <summary>
/// Consumes ride lifecycle events Ride.Handlers publishes (Ride.Core.CQRS.Events) via
/// IHikyaku.Publish and relays them to connected SignalR clients. Ride and Hub are separate
/// processes, so Kaido's remote notification fan-out over RabbitMQ is what actually delivers
/// these — the direct IHubContext&lt;VoyagerHub, IVoyagerShareClient&gt; injection this replaced
/// only ever reached clients connected to Ride's own (client-less) SignalR endpoint.
/// </summary>
public class NewRideRequestedHandler(IHubContext<VoyagerHub, IVoyagerShareClient> hub) : INotificationHandler<NewRideRequested>
{
  // Targets the driver's personal group, not the ride group: at Requested status nobody has
  // called JoinRideGroup yet (there's no active ride to authorize it against), so ride_{rideId}
  // would have zero members and the notification would be silently dropped.
  public Task Handle(NewRideRequested notification, CancellationToken cancellationToken) =>
    hub.Clients.Group($"user_{notification.DriverId}").SendToDriverNewRideRequest(notification.RideId);
}

public class RideAcceptedHandler(IHubContext<VoyagerHub, IVoyagerShareClient> hub) : INotificationHandler<RideAccepted>
{
  public Task Handle(RideAccepted notification, CancellationToken cancellationToken) =>
    hub.Clients.Group($"ride_{notification.RideId}").SendToRiderRideAccepted(notification.RideId);
}

public class RideCancelledHandler(IHubContext<VoyagerHub, IVoyagerShareClient> hub) : INotificationHandler<RideCancelled>
{
  // Personal groups, not ride_{RideId}: a ride cancelled at Requested status was never joinable
  // (JoinRideGroup authorizes against an *active* ride), so the ride group is empty and the
  // driver — still driving to a pickup that no longer exists — was never told. Same reasoning as
  // NewRideRequestedHandler above. Two distinct user groups, so nobody is notified twice.
  public Task Handle(RideCancelled notification, CancellationToken cancellationToken) =>
    hub.Clients.Groups($"user_{notification.DriverId}", $"user_{notification.UserId}")
      .SendToDriverRideCancel(notification.RideId);
}

public class RideCompletedHandler(IHubContext<VoyagerHub, IVoyagerShareClient> hub) : INotificationHandler<RideCompleted>
{
  public Task Handle(RideCompleted notification, CancellationToken cancellationToken) =>
    hub.Clients.Group($"ride_{notification.RideId}").SendToRiderRideCompleted(notification.RideId);
}

public class DriverRatingReceivedHandler(IHubContext<VoyagerHub, IVoyagerShareClient> hub) : INotificationHandler<DriverRatingReceived>
{
  public Task Handle(DriverRatingReceived notification, CancellationToken cancellationToken) =>
    hub.Clients.Group($"ride_{notification.RideId}").SendToDriverNewRateReceived(notification.Rating);
}

public class RiderRatingReceivedHandler(IHubContext<VoyagerHub, IVoyagerShareClient> hub) : INotificationHandler<RiderRatingReceived>
{
  public Task Handle(RiderRatingReceived notification, CancellationToken cancellationToken) =>
    hub.Clients.Group($"ride_{notification.RideId}").SendToRiderNewRateReceived(notification.Rating);
}
