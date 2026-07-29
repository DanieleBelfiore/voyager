using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hub.Core.Ports.Secondary;

public interface IRideEtaQuery
{
  Task<RideEta> GetEtaAsync(Guid rideId, CancellationToken cancellationToken);
}

public class RideEta
{
  public int? EstimatedArrivalMinutes { get; set; }
  public double? DistanceKm { get; set; }
}
