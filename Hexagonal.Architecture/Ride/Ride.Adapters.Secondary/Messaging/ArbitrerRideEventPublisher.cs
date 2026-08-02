using System;
using System.Threading;
using System.Threading.Tasks;
using Ride.Core.Ports.Secondary;
using MediatR;
using Voyager.Contracts.Ride;

namespace Ride.Adapters.Secondary.Messaging;

public class ArbitrerRideEventPublisher(IMediator mediator) : IRideEventPublisher
{
  public Task NewRideRequestedAsync(Guid rideId, Guid driverId, CancellationToken cancellationToken) =>
    mediator.Publish(new NewRideRequested { RideId = rideId, DriverId = driverId }, cancellationToken);

  public Task RideAcceptedAsync(Guid rideId, CancellationToken cancellationToken) =>
    mediator.Publish(new RideAccepted { RideId = rideId }, cancellationToken);

  public Task RideCancelledAsync(Guid rideId, Guid driverId, Guid userId, CancellationToken cancellationToken) =>
    mediator.Publish(new RideCancelled { RideId = rideId, DriverId = driverId, UserId = userId }, cancellationToken);

  public Task RideCompletedAsync(Guid rideId, CancellationToken cancellationToken) =>
    mediator.Publish(new RideCompleted { RideId = rideId }, cancellationToken);

  public Task DriverRatingReceivedAsync(Guid rideId, int rating, CancellationToken cancellationToken) =>
    mediator.Publish(new DriverRatingReceived { RideId = rideId, Rating = rating }, cancellationToken);

  public Task RiderRatingReceivedAsync(Guid rideId, int rating, CancellationToken cancellationToken) =>
    mediator.Publish(new RiderRatingReceived { RideId = rideId, Rating = rating }, cancellationToken);
}
