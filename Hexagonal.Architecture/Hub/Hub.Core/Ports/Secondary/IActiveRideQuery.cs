using System;
using System.Threading;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace Hub.Core.Ports.Secondary;

public interface IActiveRideQuery
{
  Task<ActiveRide> GetActiveRideForDriverAsync(Guid driverId, CancellationToken cancellationToken);
}

public class ActiveRide
{
  public Guid Id { get; set; }
  public Point PickupLocation { get; set; }
}
