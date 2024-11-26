using System;
using System.Linq;
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
  public class RateDriverHandler(IRideContext db, IMediator mediator, IHubContext<VoyagerHub, IVoyagerShareClient> hub) : IRequestHandler<RateDriver>
  {
    public async Task Handle(RateDriver request, CancellationToken cancellationToken)
    {
      var r = await db.Rides.AsNoTracking().FirstOrDefaultAsync(f => f.Id == request.RideId, cancellationToken) ?? throw new Exception("ride_not_found");

      var rides = await mediator.Send(new GetRideDriverHistory { DriverId = r.DriverId, Take = -1 }, cancellationToken);

      await mediator.Send(new UpdateUserRating { UserId = r.DriverId, Rating = request.Rating, Rides = rides.Count }, cancellationToken);

      var ride = rides.FirstOrDefault();
      if (ride == null)
        return;

      await hub.Clients.Group($"ride_{ride.Id}").SendToDriverNewRateReceived(request.Rating);
    }
  }
}
