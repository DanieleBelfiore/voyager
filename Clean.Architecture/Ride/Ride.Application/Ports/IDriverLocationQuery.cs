using System;
using System.Threading;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace Ride.Application.Ports;

/// <summary>
/// Port for the cross-service driver location lookup owned by the Driver bounded context.
/// </summary>
public interface IDriverLocationQuery
{
  Task<Point> GetLocationAsync(Guid driverId, CancellationToken cancellationToken);
}
