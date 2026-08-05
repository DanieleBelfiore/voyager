using System;
using System.Threading;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace Hub.Core.Ports.Secondary;

public interface IActiveRideQuery
{
  Task<ActiveRide> GetActiveRideForDriverAsync(Guid driverId, CancellationToken cancellationToken);

  Task<ActiveRide> GetActiveRideForParticipantAsync(Guid participantId, CancellationToken cancellationToken);
}

public class ActiveRide
{
  public Guid Id { get; set; }
  public Point PickupLocation { get; set; }

  /// <summary>True once the trip itself is under way — see Voyager.Contracts.Ride.ActiveRideInfo.
  /// Hub needs it to stop treating PickupLocation as a destination once the trip is under way.
  /// </summary>
  public bool HasStarted { get; set; }
}
