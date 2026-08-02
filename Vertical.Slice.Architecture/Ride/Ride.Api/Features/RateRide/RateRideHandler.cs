using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ride.Api.Persistence;
using Voyager.Contracts.Identity;
using Voyager.Contracts.Ride;

namespace Ride.Api.Features.RateRide;

/// <summary>No IRatingUpdateService port — recomputes the rider's rating by sending the shared UpdateUserRating contract to Identity via IMediator directly.</summary>
public class RateRideHandler(RideDbContext db, IMediator mediator) : IRequestHandler<RateRide>
{
  public async Task Handle(RateRide request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.RideId, cancellationToken)
      ?? throw new KeyNotFoundException("ride_not_found");

    if (ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    await mediator.Send(new UpdateUserRating { UserId = ride.UserId, Rating = request.Rating }, cancellationToken);

    await mediator.Publish(new RiderRatingReceived { RideId = ride.Id, Rating = request.Rating }, cancellationToken);
  }
}
