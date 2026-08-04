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
  /// to pickup": Start overwrites PickupLocation with the driver's own position at that moment,
  /// so afterwards that distance is how far they have driven, which sat under the arrival
  /// threshold for the first several hundred metres and re-fired the arrival push mid-trip.
  /// </summary>
  public bool HasStarted { get; set; }
}
