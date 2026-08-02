using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ride.Api.Persistence;
using Voyager.Contracts.Driver;
using Voyager.Contracts.Ride;

namespace Ride.Api.Features.CancelRide;

public class CancelRideHandler(RideDbContext db, IMediator mediator) : IRequestHandler<CancelRide>
{
  public async Task Handle(CancelRide request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken) ?? throw new KeyNotFoundException("no_ride_found");

    if (ride.UserId != request.CallerId && ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    var heldTheDriver = ride.Cancel(request.CancellationReason);

    await db.SaveChangesAsync(cancellationToken);

    // Only when this ride was the one holding the driver. Cancelling a stale Requested ride used
    // to release a driver who was already mid-trip on someone else's — see Ride.Cancel.
    if (heldTheDriver)
      await mediator.Send(new MarkDriverAvailable { DriverId = ride.DriverId }, cancellationToken);

    await mediator.Publish(new RideCancelled { RideId = ride.Id, DriverId = ride.DriverId, UserId = ride.UserId }, cancellationToken);
  }
}
