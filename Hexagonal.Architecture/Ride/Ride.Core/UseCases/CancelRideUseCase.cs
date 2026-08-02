using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ride.Core.Ports.Secondary;
using Ride.Core.Ports.Primary;

namespace Ride.Core.UseCases;

public class CancelRideUseCase(IRideRepository repository, IRideEventPublisher events, IDriverAvailabilityNotifier availability) : ICancelRideUseCase
{
  public async Task Handle(CancelRide request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetByIdAsync(request.Id, cancellationToken) ?? throw new KeyNotFoundException("no_ride_found");

    if (ride.UserId != request.CallerId && ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    ride.Cancel(request.CancellationReason);

    await repository.SaveChangesAsync(cancellationToken);

    // Always Available, whether or not Accept had already moved them to OnRide — a no-op
    // status write is cheaper than branching on ride.Status to decide if it's needed.
    await availability.MarkAvailableAsync(ride.DriverId, cancellationToken);

    await events.RideCancelledAsync(ride.Id, cancellationToken);
  }
}
