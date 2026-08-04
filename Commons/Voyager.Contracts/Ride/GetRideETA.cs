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
}

public class RideETAInfo
{
  public int? EstimatedArrivalMinutes { get; set; }
  public double? DistanceKm { get; set; }
}
