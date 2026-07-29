using System;
using System.Threading;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace Ride.Core.Ports.Secondary;

public interface IDriverLocationQuery
{
  Task<Point> GetLocationAsync(Guid driverId, CancellationToken cancellationToken);
}
