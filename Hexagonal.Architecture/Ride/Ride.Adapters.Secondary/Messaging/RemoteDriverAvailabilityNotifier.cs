using System;
using System.Threading;
using System.Threading.Tasks;
using Hikyaku;
using Ride.Core.Ports.Secondary;
using Voyager.Contracts.Driver;

namespace Ride.Adapters.Secondary.Messaging;

public class RemoteDriverAvailabilityNotifier(IHikyaku mediator) : IDriverAvailabilityNotifier
{
  public Task MarkOnRideAsync(Guid driverId, CancellationToken cancellationToken) =>
    mediator.Send(new MarkDriverOnRide { DriverId = driverId }, cancellationToken);

  public Task MarkAvailableAsync(Guid driverId, CancellationToken cancellationToken) =>
    mediator.Send(new MarkDriverAvailable { DriverId = driverId }, cancellationToken);
}
