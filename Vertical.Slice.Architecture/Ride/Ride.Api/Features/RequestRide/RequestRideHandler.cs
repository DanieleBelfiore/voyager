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

    await db.SaveChangesAsync(cancellationToken);

    await mediator.Publish(new NewRideRequested { RideId = ride.Id }, cancellationToken);

    return RideDetailsResponse.From(ride);
  }
}
