using System;
using System.Threading;
using System.Threading.Tasks;
using Hub.Core.Ports.Secondary;
using Hikyaku;
using Voyager.Contracts.Ride;

namespace Hub.Adapters.Secondary.Messaging;

public class RemoteActiveRideQuery(IHikyaku mediator) : IActiveRideQuery
{
  public async Task<ActiveRide> GetActiveRideForDriverAsync(Guid driverId, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new GetActiveRide { DriverId = driverId }, cancellationToken);

    return result == null ? null : new ActiveRide { Id = result.Id, PickupLocation = result.PickupLocation, HasStarted = result.HasStarted };
  }

  public async Task<ActiveRide> GetActiveRideForParticipantAsync(Guid participantId, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new GetActiveRide { DriverId = participantId, UserId = participantId }, cancellationToken);

    return result == null ? null : new ActiveRide { Id = result.Id, PickupLocation = result.PickupLocation, HasStarted = result.HasStarted };
  }
}
