using System;
using MediatR;
using NetTopologySuite.Geometries;

namespace Voyager.Contracts.Driver;

/// <summary>
/// Cross-service query: a driver's last known location, owned by the Driver bounded context.
/// A slimmer sibling of Driver's own GetDriverStatus query — Ride's ETA calculation and Hub's
/// arrival-distance check only need the location, not the full driver status payload.
/// </summary>
public class GetDriverLocation : IRequest<DriverLocationInfo>
{
  public Guid DriverId { get; set; }
}

public class DriverLocationInfo
{
  public Point LastLocation { get; set; }
}
