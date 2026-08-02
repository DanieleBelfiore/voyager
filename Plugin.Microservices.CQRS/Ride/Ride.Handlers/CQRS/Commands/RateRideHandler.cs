using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Core.Exceptions;
using Identity.Core.CQRS.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ride.Core.CQRS.Commands;
using Ride.Core.CQRS.Events;
using Ride.Handlers.Interfaces;

namespace Ride.Handlers.CQRS.Commands;

public class RateRideHandler(IRideContext db, IMediator mediator) : IRequestHandler<RateRide>
{
  public async Task Handle(RateRide request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.AsNoTracking().FirstOrDefaultAsync(f => f.Id == request.RideId, cancellationToken) ?? throw new NotFoundException("ride_not_found");

    if (ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    await mediator.Send(new UpdateUserRating { UserId = ride.UserId, Rating = request.Rating }, cancellationToken);

    await mediator.Publish(new RiderRatingReceived { RideId = ride.Id, Rating = request.Rating }, cancellationToken);
  }
}
