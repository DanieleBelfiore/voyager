using System;
using Hikyaku;

namespace Voyager.Contracts.Ride;

/// <summary>
/// Cross-service query: ETA for a ride, owned by the Ride bounded context. Sent remotely by
/// Hub after pushing a driver's new location to connected riders.
/// </summary>
public class GetRideETA : IRequest<RideETAInfo>
{
  public Guid Id { get; set; }

  /// <summary>
  /// The user Hub is acting for — the driver whose position triggered the push. The handler
  /// checks it against the ride's participants, so this query is no more readable over the broker
  /// than the equivalent HTTP endpoint is: without it, any caller reaching the broker could ask
  /// for any ride's ETA by id and learn where that driver is.
  /// </summary>
  public Guid CallerId { get; set; }
}

public class RideETAInfo
{
  public int? EstimatedArrivalMinutes { get; set; }
  public double? DistanceKm { get; set; }
}
