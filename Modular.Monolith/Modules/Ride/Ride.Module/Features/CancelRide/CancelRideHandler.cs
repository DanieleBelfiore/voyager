using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ride.Module.Persistence;
using Voyager.Contracts.Ride;

namespace Ride.Module.Features.CancelRide;

internal class CancelRideHandler(RideDbContext db, IMediator mediator) : IRequestHandler<CancelRide>
{
  public async Task Handle(CancelRide request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken) ?? throw new Exception("no_ride_found");

    ride.Cancel(request.CancellationReason);

    await db.SaveChangesAsync(cancellationToken);

    await mediator.Publish(new RideCancelled { RideId = ride.Id }, cancellationToken);
  }
}
