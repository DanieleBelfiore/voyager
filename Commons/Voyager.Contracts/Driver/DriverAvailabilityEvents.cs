using System;
using Hikyaku;

namespace Voyager.Contracts.Driver;

/// <summary>
/// Cross-service command: mark a driver unavailable for new matches because they've just been
/// assigned a ride. Sent remotely by Ride on Accept — otherwise the driver keeps ranking in
/// driver search while already committed to a ride.
/// </summary>
public class MarkDriverOnRide : IRequest
{
  public Guid DriverId { get; set; }
}

/// <summary>
/// Cross-service command: mark a driver available for new matches again, because their ride
/// ended (Complete) or never started (Cancel). Sent remotely by Ride.
/// </summary>
public class MarkDriverAvailable : IRequest
{
  public Guid DriverId { get; set; }
}
