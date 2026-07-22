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
  Task NewRideRequestedAsync(Guid rideId, CancellationToken cancellationToken);
  Task RideAcceptedAsync(Guid rideId, CancellationToken cancellationToken);
  Task RideCancelledAsync(Guid rideId, CancellationToken cancellationToken);
  Task RideCompletedAsync(Guid rideId, CancellationToken cancellationToken);
  Task DriverRatingReceivedAsync(Guid rideId, int rating, CancellationToken cancellationToken);
  Task RiderRatingReceivedAsync(Guid rideId, int rating, CancellationToken cancellationToken);
}
