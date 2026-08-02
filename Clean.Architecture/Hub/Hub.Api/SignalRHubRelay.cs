using System;
using System.Threading;
using System.Threading.Tasks;
using Hub.Application.Ports;
using Microsoft.AspNetCore.SignalR;
using NetTopologySuite.Geometries;

namespace Hub.Api;

/// <summary>
/// Implements Application's IHubRelay port over SignalR. Lives in Api (not Infrastructure)
/// because IHubContext&lt;VoyagerHub, IVoyagerShareClient&gt; is generic over the concrete Hub
/// class, which is itself a presentation type — Infrastructure would have to depend on Api to
/// implement this otherwise, which is backwards. See Hub.Infrastructure's README note.
/// </summary>
public class SignalRHubRelay(IHubContext<VoyagerHub, IVoyagerShareClient> hub) : IHubRelay
{
  private static string GroupFor(Guid rideId) => $"ride_{rideId}";
  private static string UserGroupFor(Guid userId) => $"user_{userId}";

  // Targets the driver's personal group, not the ride group: at Requested status nobody has
  // called JoinRideGroup yet (there's no active ride to authorize it against), so ride_{rideId}
  // would have zero members and the notification would be silently dropped.
  public Task SendToDriverNewRideRequest(Guid rideId, Guid driverId, CancellationToken cancellationToken) =>
    hub.Clients.Group(UserGroupFor(driverId)).SendToDriverNewRideRequest(rideId);

  public Task SendToDriverRideCancel(Guid rideId, CancellationToken cancellationToken) =>
    hub.Clients.Group(GroupFor(rideId)).SendToDriverRideCancel(rideId);

  public Task SendToDriverNewRateReceived(Guid rideId, int rating, CancellationToken cancellationToken) =>
    hub.Clients.Group(GroupFor(rideId)).SendToDriverNewRateReceived(rating);

  public Task SendToRiderNewDriverLocation(Guid rideId, Point location, CancellationToken cancellationToken) =>
    hub.Clients.Group(GroupFor(rideId)).SendToRiderNewDriverLocation(location);

  public Task SendToRiderDriverArrival(Guid rideId, CancellationToken cancellationToken) =>
    hub.Clients.Group(GroupFor(rideId)).SendToRiderDriverArrival(rideId);

  public Task SendToRiderNewETA(Guid rideId, int? estimatedArrivalMinutes, double? distanceKm, CancellationToken cancellationToken) =>
    hub.Clients.Group(GroupFor(rideId)).SendToRiderNewETA(estimatedArrivalMinutes, distanceKm);

  public Task SendToRiderNewRateReceived(Guid rideId, int rating, CancellationToken cancellationToken) =>
    hub.Clients.Group(GroupFor(rideId)).SendToRiderNewRateReceived(rating);

  public Task SendToRiderRideAccepted(Guid rideId, CancellationToken cancellationToken) =>
    hub.Clients.Group(GroupFor(rideId)).SendToRiderRideAccepted(rideId);

  public Task SendToRiderRideCompleted(Guid rideId, CancellationToken cancellationToken) =>
    hub.Clients.Group(GroupFor(rideId)).SendToRiderRideCompleted(rideId);
}
