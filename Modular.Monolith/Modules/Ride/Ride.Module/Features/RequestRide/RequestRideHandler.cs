using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ride.Module.Entities;
using Ride.Module.Persistence;
using Ride.Module.Shared;
using Voyager.Contracts.Driver;
using Voyager.Contracts.Ride;
using RideEntity = Ride.Module.Entities.Ride;
using Voyager.Errors;

namespace Ride.Module.Features.RequestRide;

internal class RequestRideHandler(RideDbContext db, IMediator mediator) : IRequestHandler<RequestRide, RideDetailsResponse>
{
  public async Task<RideDetailsResponse> Handle(RequestRide request, CancellationToken cancellationToken)
  {
    // DriverId comes straight off the request body, so it is checked before a ride is created
    // against it — otherwise a rider can pin a ride onto any GUID at all, including one that
    // belongs to a non-driver or to a driver already committed to someone else's trip.
    var availability = await mediator.Send(new GetDriverAvailability { DriverId = request.DriverId }, cancellationToken);
    if (availability is not { Exists: true })
      throw new KeyNotFoundException("driver_not_found");

    if (!availability.IsAvailable)
      throw new ConflictException("driver_not_available");

    var alreadyRequested = await db.Rides.AsNoTracking().AnyAsync(r =>
      r.UserId == request.UserId && (r.Status == RideStatus.Requested || r.Status == RideStatus.DriverAssigned || r.Status == RideStatus.InProgress),
      cancellationToken);
    if (alreadyRequested)
      throw new ConflictException("ride_already_in_progress");

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
      throw new ConflictException("ride_already_in_progress");
    }

    await mediator.Publish(new NewRideRequested { RideId = ride.Id, DriverId = ride.DriverId }, cancellationToken);

    return RideDetailsResponse.From(ride);
  }
}
