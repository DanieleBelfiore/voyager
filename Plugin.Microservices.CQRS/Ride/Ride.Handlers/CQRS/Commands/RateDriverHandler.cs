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

public class RateDriverHandler(IRideContext db, IMediator mediator, IHubContext<VoyagerHub, IVoyagerShareClient> hub) : IRequestHandler<RateDriver>
{
  public async Task Handle(RateDriver request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.AsNoTracking().FirstOrDefaultAsync(f => f.Id == request.RideId, cancellationToken) ?? throw new Exception("ride_not_found");

    if (ride.UserId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    await mediator.Send(new UpdateUserRating { UserId = ride.DriverId, Rating = request.Rating }, cancellationToken);

    await hub.Clients.Group($"ride_{ride.Id}").SendToDriverNewRateReceived(request.Rating);
  }
}
