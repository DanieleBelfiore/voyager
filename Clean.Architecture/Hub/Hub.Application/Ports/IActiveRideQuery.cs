using System;
using System.Threading;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace Hub.Application.Ports;

/// <summary>Port for the cross-service active-ride lookup owned by the Ride bounded context.</summary>
public interface IActiveRideQuery
{
  Task<ActiveRide> GetActiveRideForDriverAsync(Guid driverId, CancellationToken cancellationToken);

  Task<ActiveRide> GetActiveRideForParticipantAsync(Guid participantId, CancellationToken cancellationToken);
}

public class ActiveRide
{
  public Guid Id { get; set; }
  public Point PickupLocation { get; set; }
}
