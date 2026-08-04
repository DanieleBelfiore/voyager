using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hikyaku;
using Microsoft.EntityFrameworkCore;
using Ride.Api.Persistence;
using Voyager.Contracts.Driver;
using Voyager.Contracts.Ride;

namespace Ride.Api.Features.AcceptRide;

public class AcceptRideHandler(RideDbContext db, IHikyaku mediator) : IRequestHandler<AcceptRide>
{
  public async Task Handle(AcceptRide request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.FirstOrDefaultAsync(r => r.Id == request.RideId, cancellationToken) ?? throw new KeyNotFoundException("no_ride_found");

    ride.Accept(request.DriverId);

    await db.SaveChangesAsync(cancellationToken);

    // Otherwise the driver keeps ranking in driver search while already committed to a ride.
    await mediator.Send(new MarkDriverOnRide { DriverId = ride.DriverId }, cancellationToken);

    await mediator.Publish(new RideAccepted { RideId = ride.Id }, cancellationToken);
  }
}
