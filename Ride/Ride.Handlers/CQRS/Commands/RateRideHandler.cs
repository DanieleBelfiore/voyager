using System;
using System.Threading;
using System.Threading.Tasks;
using Hub.API;
using Hub.Core.Interfaces;
using Identity.Core.CQRS.Commands;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Ride.Core.CQRS.Queries;
using Ride.Handlers.Interfaces;

namespace Ride.Core.CQRS.Commands
{
  public class RateRideHandler(IRideContext db, IMediator mediator, IHubContext<VoyagerHub, IVoyagerShareClient> hub) : IRequestHandler<RateRide>
  {
    public async Task Handle(RateRide request, CancellationToken cancellationToken)
    {
      var ride = await db.Rides.AsNoTracking().FirstOrDefaultAsync(f => f.Id == request.RideId, cancellationToken) ?? throw new Exception("ride_not_found");

      var rides = await mediator.Send(new GetRideHistory { UserId = ride.UserId, Take = -1 }, cancellationToken);

      await mediator.Send(new UpdateUserRating { UserId = ride.UserId, Rating = request.Rating, Rides = rides.Count }, cancellationToken);

      await hub.Clients.Group($"ride_{ride.Id}").SendToRiderNewRateReceived(request.Rating);
    }
  }
}
