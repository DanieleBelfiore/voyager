using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hub.Application.Ports;

/// <summary>Port for the cross-service ETA lookup owned by the Ride bounded context.</summary>
public interface IRideEtaQuery
{
  Task<RideEta> GetEtaAsync(Guid rideId, CancellationToken cancellationToken);
}

public class RideEta
{
  public int? EstimatedArrivalMinutes { get; set; }
  public double? DistanceKm { get; set; }
}
