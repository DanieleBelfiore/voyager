using System;
using Hikyaku;

namespace Voyager.Contracts.Driver;

/// <summary>
/// Cross-service query: whether a driver exists and is currently free to take a ride, owned by
/// the Driver bounded context. Sent remotely by Ride before creating a ride — RequestRide takes
/// the DriverId straight from the caller's request body, so without this the rider can pin a
/// ride onto any GUID at all, including one that belongs to a non-driver or to a driver already
/// committed to someone else's trip.
/// </summary>
public class GetDriverAvailability : IRequest<DriverAvailabilityInfo>
{
  public Guid DriverId { get; set; }
}

public class DriverAvailabilityInfo
{
  public bool Exists { get; set; }
  public bool IsAvailable { get; set; }
}
