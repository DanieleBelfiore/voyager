using System;
using System.Threading;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace Hub.Core.Ports.Secondary;

public interface IDriverLocationUpdater
{
  Task UpdateLocationAsync(Guid driverId, Point location, CancellationToken cancellationToken);
}
