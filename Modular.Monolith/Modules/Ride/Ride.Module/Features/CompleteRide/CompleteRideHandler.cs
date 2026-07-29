using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ride.Module.Persistence;
using Voyager.Contracts.Ride;

namespace Ride.Module.Features.CompleteRide;

internal class CompleteRideHandler(RideDbContext db, IMediator mediator) : IRequestHandler<CompleteRide>
{
  public async Task Handle(CompleteRide request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken) ?? throw new Exception("no_ride_found");

    ride.Complete(request.Location, request.Price);

    await db.SaveChangesAsync(cancellationToken);

    await mediator.Publish(new RideCompleted { RideId = ride.Id }, cancellationToken);
  }
}
