using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ride.Core.Ports.Secondary;

/// <summary>
/// Port for the cross-service driver availability lookup owned by the Driver bounded context.
/// RequestRide takes DriverId straight from the caller's body, so it has to be checked before
/// a ride is created against it.
/// </summary>
public interface IDriverAvailabilityQuery
{
  Task<DriverAvailability> GetAvailabilityAsync(Guid driverId, CancellationToken cancellationToken);
}

public record DriverAvailability(bool Exists, bool IsAvailable);
