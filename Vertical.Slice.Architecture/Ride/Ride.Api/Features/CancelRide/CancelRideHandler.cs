using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ride.Api.Persistence;
using Voyager.Contracts.Ride;

namespace Ride.Api.Features.CancelRide;

public class CancelRideHandler(RideDbContext db, IMediator mediator) : IRequestHandler<CancelRide>
{
  public async Task Handle(CancelRide request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken) ?? throw new Exception("no_ride_found");

    if (ride.UserId != request.CallerId && ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    ride.Cancel(request.CancellationReason);

    await db.SaveChangesAsync(cancellationToken);

    await mediator.Publish(new RideCancelled { RideId = ride.Id }, cancellationToken);
  }
}
