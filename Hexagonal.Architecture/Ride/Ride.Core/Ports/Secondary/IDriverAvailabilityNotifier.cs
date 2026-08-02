using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ride.Core.Ports.Secondary;

public interface IDriverAvailabilityNotifier
{
  Task MarkOnRideAsync(Guid driverId, CancellationToken cancellationToken);
  Task MarkAvailableAsync(Guid driverId, CancellationToken cancellationToken);
}
