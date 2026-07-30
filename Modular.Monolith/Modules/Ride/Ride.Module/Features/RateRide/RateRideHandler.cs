using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ride.Module.Entities;
using Ride.Module.Persistence;
using Voyager.Contracts.Identity;
using Voyager.Contracts.Ride;

namespace Ride.Module.Features.RateRide;

internal class RateRideHandler(RideDbContext db, IMediator mediator) : IRequestHandler<RateRide>
{
  public async Task Handle(RateRide request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.RideId, cancellationToken)
      ?? throw new Exception("ride_not_found");

    if (ride.UserId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    var rideCount = await db.Rides.AsNoTracking()
      .Where(r => r.UserId == ride.UserId && r.Status == RideStatus.Completed)
      .CountAsync(cancellationToken);

    await mediator.Send(new UpdateUserRating { UserId = ride.UserId, Rating = request.Rating, Rides = rideCount }, cancellationToken);

    await mediator.Publish(new RiderRatingReceived { RideId = ride.Id, Rating = request.Rating }, cancellationToken);
  }
}
