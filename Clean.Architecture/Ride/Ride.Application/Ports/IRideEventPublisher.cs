using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ride.Application.Ports;

/// <summary>
/// Port for publishing ride lifecycle events for Hub to relay over SignalR. Infrastructure
/// implements this via MediatR.Publish + Arbitrer's remote notification fan-out.
/// </summary>
public interface IRideEventPublisher
{
  Task NewRideRequestedAsync(Guid rideId, Guid driverId, CancellationToken cancellationToken);
  Task RideAcceptedAsync(Guid rideId, CancellationToken cancellationToken);
  /// <summary>Carries both participant ids for the same reason NewRideRequestedAsync carries
  /// driverId: a ride cancelled before the driver accepts has no joinable ride group to
  /// broadcast into. See Voyager.Contracts.Ride.RideCancelled.</summary>
  Task RideCancelledAsync(Guid rideId, Guid driverId, Guid userId, CancellationToken cancellationToken);
  Task RideCompletedAsync(Guid rideId, CancellationToken cancellationToken);
  Task DriverRatingReceivedAsync(Guid rideId, int rating, CancellationToken cancellationToken);
  Task RiderRatingReceivedAsync(Guid rideId, int rating, CancellationToken cancellationToken);
}
