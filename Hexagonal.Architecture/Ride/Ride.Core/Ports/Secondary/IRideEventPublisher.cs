using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ride.Core.Ports.Secondary;

public interface IRideEventPublisher
{
  Task NewRideRequestedAsync(Guid rideId, Guid driverId, CancellationToken cancellationToken);
  Task RideAcceptedAsync(Guid rideId, CancellationToken cancellationToken);
  Task RideCancelledAsync(Guid rideId, CancellationToken cancellationToken);
  Task RideCompletedAsync(Guid rideId, CancellationToken cancellationToken);
  Task DriverRatingReceivedAsync(Guid rideId, int rating, CancellationToken cancellationToken);
  Task RiderRatingReceivedAsync(Guid rideId, int rating, CancellationToken cancellationToken);
}
