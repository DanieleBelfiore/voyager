using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ride.Module.Persistence;
using Voyager.Contracts.Driver;
using Voyager.Contracts.Ride;

namespace Ride.Module.Features.CancelRide;

internal class CancelRideHandler(RideDbContext db, IMediator mediator) : IRequestHandler<CancelRide>
{
  public async Task Handle(CancelRide request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken) ?? throw new KeyNotFoundException("no_ride_found");

    if (ride.UserId != request.CallerId && ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    ride.Cancel(request.CancellationReason);

    await db.SaveChangesAsync(cancellationToken);

    // Always Available, whether or not Accept had already moved them to OnRide — a no-op
    // status write is cheaper than branching on ride.Status to decide if it's needed.
    await mediator.Send(new MarkDriverAvailable { DriverId = ride.DriverId }, cancellationToken);

    await mediator.Publish(new RideCancelled { RideId = ride.Id }, cancellationToken);
  }
}
