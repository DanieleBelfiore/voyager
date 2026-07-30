using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ride.Api.Entities;
using Ride.Api.Persistence;
using Ride.Api.Shared;
using Voyager.Contracts.Ride;
using RideEntity = Ride.Api.Entities.Ride;

namespace Ride.Api.Features.RequestRide;

/// <summary>No IRideEventPublisher port — publishes the shared ride-lifecycle notification via IMediator directly; Arbitrer fans it out to Hub.</summary>
public class RequestRideHandler(RideDbContext db, IMediator mediator) : IRequestHandler<RequestRide, RideDetailsResponse>
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
      // The in-memory check above can't fully close the race under concurrent requests — the
      // unique filtered index (IX_Rides_UserId_ActiveRide) is the actual guarantee.
      throw new InvalidOperationException();
    }

    await mediator.Publish(new NewRideRequested { RideId = ride.Id }, cancellationToken);

    return RideDetailsResponse.From(ride);
  }
}
