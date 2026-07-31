using System;
using System.Threading;
using System.Threading.Tasks;
using Hub.API;
using Hub.Core.Interfaces;
using Identity.Core.CQRS.Commands;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Ride.Core.CQRS.Commands;
using Ride.Handlers.Interfaces;

namespace Ride.Handlers.CQRS.Commands;

public class RateRideHandler(IRideContext db, IMediator mediator, IHubContext<VoyagerHub, IVoyagerShareClient> hub) : IRequestHandler<RateRide>
{
  public async Task Handle(RateRide request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.AsNoTracking().FirstOrDefaultAsync(f => f.Id == request.RideId, cancellationToken) ?? throw new Exception("ride_not_found");

    if (ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    await mediator.Send(new UpdateUserRating { UserId = ride.UserId, Rating = request.Rating }, cancellationToken);

    await hub.Clients.Group($"ride_{ride.Id}").SendToRiderNewRateReceived(request.Rating);
  }
}
