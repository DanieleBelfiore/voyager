using System;
using Hikyaku;
using NetTopologySuite.Geometries;

namespace Voyager.Contracts.Ride;

/// <summary>
/// Cross-service query: the ride currently in progress for a driver or rider, owned by the
/// Ride bounded context. Sent remotely by Hub to know which ride group to push updates to.
/// </summary>
public class GetActiveRide : IRequest<ActiveRideInfo>
{
  public Guid? DriverId { get; set; }
  public Guid? UserId { get; set; }
}

public class ActiveRideInfo
{
  public Guid Id { get; set; }
  public Point PickupLocation { get; set; }

  /// <summary>True once the trip itself is under way. Hub needs it to stop measuring "distance
  /// to pickup": afterwards the driver is moving away from it, so that distance drifts back under
  /// the arrival threshold near the start of the trip and re-fired the arrival push mid-trip.
  /// </summary>
  public bool HasStarted { get; set; }
}
