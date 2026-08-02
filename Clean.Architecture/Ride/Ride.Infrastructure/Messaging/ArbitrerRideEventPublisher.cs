using System;
using System.Threading;
using System.Threading.Tasks;
using Ride.Application.Ports;
using MediatR;
using Voyager.Contracts.Ride;

namespace Ride.Infrastructure.Messaging;

/// <summary>
/// Publishes ride lifecycle events as MediatR notifications. No local handler exists in Ride
/// itself, so Arbitrer's remote notification fan-out (InferLocalNotifications) delivers them
/// to Hub over RabbitMQ — the service that actually owns connected SignalR clients. Replaces
/// the Plugin.Microservices.CQRS variant's direct (and non-functional, cross-process)
/// IHubContext&lt;VoyagerHub,...&gt; injection — see Voyager.Contracts/Ride/RideEvents.cs.
/// </summary>
public class ArbitrerRideEventPublisher(IMediator mediator) : IRideEventPublisher
{
  public Task NewRideRequestedAsync(Guid rideId, Guid driverId, CancellationToken cancellationToken) =>
    mediator.Publish(new NewRideRequested { RideId = rideId, DriverId = driverId }, cancellationToken);

  public Task RideAcceptedAsync(Guid rideId, CancellationToken cancellationToken) =>
    mediator.Publish(new RideAccepted { RideId = rideId }, cancellationToken);

  public Task RideCancelledAsync(Guid rideId, CancellationToken cancellationToken) =>
    mediator.Publish(new RideCancelled { RideId = rideId }, cancellationToken);

  public Task RideCompletedAsync(Guid rideId, CancellationToken cancellationToken) =>
    mediator.Publish(new RideCompleted { RideId = rideId }, cancellationToken);

  public Task DriverRatingReceivedAsync(Guid rideId, int rating, CancellationToken cancellationToken) =>
    mediator.Publish(new DriverRatingReceived { RideId = rideId, Rating = rating }, cancellationToken);

  public Task RiderRatingReceivedAsync(Guid rideId, int rating, CancellationToken cancellationToken) =>
    mediator.Publish(new RiderRatingReceived { RideId = rideId, Rating = rating }, cancellationToken);
}
