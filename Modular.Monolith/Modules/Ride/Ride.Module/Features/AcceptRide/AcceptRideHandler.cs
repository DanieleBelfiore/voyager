using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ride.Module.Persistence;
using Voyager.Contracts.Ride;

namespace Ride.Module.Features.AcceptRide;

internal class AcceptRideHandler(RideDbContext db, IMediator mediator) : IRequestHandler<AcceptRide>
{
  public async Task Handle(AcceptRide request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.FirstOrDefaultAsync(r => r.Id == request.RideId, cancellationToken) ?? throw new Exception("no_ride_found");

    ride.Accept(request.DriverId);

    await db.SaveChangesAsync(cancellationToken);

    await mediator.Publish(new RideAccepted { RideId = ride.Id }, cancellationToken);
  }
}
