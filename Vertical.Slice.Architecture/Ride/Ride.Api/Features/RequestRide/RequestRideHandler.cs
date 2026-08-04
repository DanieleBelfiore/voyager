using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hikyaku;
using Microsoft.EntityFrameworkCore;
using Ride.Api.Entities;
using Ride.Api.Persistence;
using Ride.Api.Shared;
using Voyager.Contracts.Driver;
using Voyager.Contracts.Ride;
using RideEntity = Ride.Api.Entities.Ride;
using Voyager.Errors;

namespace Ride.Api.Features.RequestRide;

/// <summary>No IRideEventPublisher port — publishes the shared ride-lifecycle notification via IHikyaku directly; Kaido fans it out to Hub.</summary>
public class RequestRideHandler(RideDbContext db, IHikyaku mediator) : IRequestHandler<RequestRide, RideDetailsResponse>
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
      // The in-memory check above can't fully close the race under concurrent requests — the
      // unique filtered index (IX_Rides_UserId_ActiveRide) is the actual guarantee.
      throw new ConflictException("ride_already_in_progress");
    }

    await mediator.Publish(new NewRideRequested { RideId = ride.Id, DriverId = ride.DriverId }, cancellationToken);

    return RideDetailsResponse.From(ride);
  }
}
