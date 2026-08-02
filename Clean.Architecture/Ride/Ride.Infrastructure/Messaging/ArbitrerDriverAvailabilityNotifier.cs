using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Ride.Application.Ports;
using Voyager.Contracts.Driver;

namespace Ride.Infrastructure.Messaging;

/// <summary>
/// Sends the shared driver-availability commands through the local MediatR pipeline. No
/// handler for them is registered in Ride itself, so Arbitrer's implicit-remote behaviour
/// routes them over RabbitMQ to Driver, the service that owns that state.
/// </summary>
public class ArbitrerDriverAvailabilityNotifier(IMediator mediator) : IDriverAvailabilityNotifier
{
  public Task MarkOnRideAsync(Guid driverId, CancellationToken cancellationToken) =>
    mediator.Send(new MarkDriverOnRide { DriverId = driverId }, cancellationToken);

  public Task MarkAvailableAsync(Guid driverId, CancellationToken cancellationToken) =>
    mediator.Send(new MarkDriverAvailable { DriverId = driverId }, cancellationToken);
}
