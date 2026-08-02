using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ride.Application.Ports;
using MediatR;

namespace Ride.Application.CQRS.Commands;

public class CancelRideHandler(IRideRepository repository, IRideEventPublisher events, IDriverAvailabilityNotifier availability) : IRequestHandler<CancelRide>
{
  public async Task Handle(CancelRide request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetByIdAsync(request.Id, cancellationToken) ?? throw new KeyNotFoundException("no_ride_found");

    if (ride.UserId != request.CallerId && ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    var heldTheDriver = ride.Cancel(request.CancellationReason);

    await repository.SaveChangesAsync(cancellationToken);

    // Only when this ride was the one holding the driver. Cancelling a stale Requested ride used
    // to release a driver who was already mid-trip on someone else's — see Ride.Cancel.
    if (heldTheDriver)
      await availability.MarkAvailableAsync(ride.DriverId, cancellationToken);

    await events.RideCancelledAsync(ride.Id, ride.DriverId, ride.UserId, cancellationToken);
  }
}
