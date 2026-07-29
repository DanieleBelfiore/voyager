using System;
using System.Threading;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace Hub.Application.Ports;

/// <summary>Port for the cross-service location update owned by the Driver bounded context.</summary>
public interface IDriverLocationUpdater
{
  Task UpdateLocationAsync(Guid driverId, Point location, CancellationToken cancellationToken);
}
