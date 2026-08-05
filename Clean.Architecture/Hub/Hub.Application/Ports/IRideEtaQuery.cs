using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hub.Application.Ports;

/// <summary>Port for the cross-service ETA lookup owned by the Ride bounded context.</summary>
public interface IRideEtaQuery
{
  /// <summary>callerId is the driver Hub is acting for; Ride checks it against the ride's
  /// participants before answering.</summary>
  Task<RideEta> GetEtaAsync(Guid rideId, Guid callerId, CancellationToken cancellationToken);
}

public class RideEta
{
  public int? EstimatedArrivalMinutes { get; set; }
  public double? DistanceKm { get; set; }
}
