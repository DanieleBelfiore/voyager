using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ride.Application.Ports;

/// <summary>
/// Port for telling Driver a driver's availability changed as a side effect of the ride
/// lifecycle. Infrastructure implements this via MediatR.Send + Arbitrer's remote dispatch.
/// </summary>
public interface IDriverAvailabilityNotifier
{
  Task MarkOnRideAsync(Guid driverId, CancellationToken cancellationToken);
  Task MarkAvailableAsync(Guid driverId, CancellationToken cancellationToken);
}
