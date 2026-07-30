using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ride.Module.Entities;
using Ride.Module.Persistence;
using Ride.Module.Shared;
using Voyager.Contracts.Ride;
using RideEntity = Ride.Module.Entities.Ride;

namespace Ride.Module.Features.RequestRide;

internal class RequestRideHandler(RideDbContext db, IMediator mediator) : IRequestHandler<RequestRide, RideDetailsResponse>
{
  public async Task<RideDetailsResponse> Handle(RequestRide request, CancellationToken cancellationToken)
  {
    var alreadyRequested = await db.Rides.AsNoTracking().AnyAsync(r =>
      r.UserId == request.UserId && (r.Status == RideStatus.Requested || r.Status == RideStatus.DriverAssigned || r.Status == RideStatus.InProgress),
      cancellationToken);
    if (alreadyRequested)
      throw new InvalidOperationException();

    var ride = new RideEntity(request.UserId, request.DriverId, request.PickupLocation, request.DropoffLocation);

    db.Rides.Add(ride);

    try
    {
      await db.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException)
    {
      // Two concurrent requests can both pass the check above; the unique filtered index on
      // Rides.UserId (active statuses only) is the DB-enforced backstop for that race.
      throw new InvalidOperationException();
    }

    await mediator.Publish(new NewRideRequested { RideId = ride.Id }, cancellationToken);

    return RideDetailsResponse.From(ride);
  }
}
