using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ride.Api.Persistence;
using Voyager.Contracts.Identity;
using Voyager.Contracts.Ride;

namespace Ride.Api.Features.RateDriver;

public class RateDriverHandler(RideDbContext db, IMediator mediator) : IRequestHandler<RateDriver>
{
  public async Task Handle(RateDriver request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.RideId, cancellationToken)
      ?? throw new Exception("ride_not_found");

    if (ride.UserId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    await mediator.Send(new UpdateUserRating { UserId = ride.DriverId, Rating = request.Rating }, cancellationToken);

    await mediator.Publish(new DriverRatingReceived { RideId = ride.Id, Rating = request.Rating }, cancellationToken);
  }
}
