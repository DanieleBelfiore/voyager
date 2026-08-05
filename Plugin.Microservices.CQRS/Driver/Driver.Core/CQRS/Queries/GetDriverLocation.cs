using System;
using Hikyaku;
using NetTopologySuite.Geometries;

namespace Driver.Core.CQRS.Queries
{
  /// <summary>
  /// Cross-service query: a driver's last known location. Ride's ETA calculation needs the point
  /// and nothing else. Deliberately narrower than <see cref="GetDriverStatus"/>, which is a
  /// user-facing query gated on the caller owning the driver record — reusing that one here would
  /// force a service-to-service call to carry an end-user identity it does not have.
  /// </summary>
  public class GetDriverLocation : IRequest<DriverLocationInfo>
  {
    public Guid DriverId { get; set; }
  }

  public class DriverLocationInfo
  {
    public Point LastLocation { get; set; }
  }
}
