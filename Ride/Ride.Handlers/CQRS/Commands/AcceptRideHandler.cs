using System;
using System.Threading;
using System.Threading.Tasks;
using Hub.API;
using Hub.Core.Interfaces;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Ride.Core.CQRS.Commands;
using Ride.Core.Enums;
using Ride.Handlers.Interfaces;

namespace Ride.Handlers.CQRS.Commands
{
  public class AcceptRideHandler(IRideContext db, IHubContext<VoyagerHub, IVoyagerShareClient> hub) : IRequestHandler<AcceptRide>
  {
    public async Task Handle(AcceptRide request, CancellationToken cancellationToken)
    {
      var ride = await db.Rides.FirstOrDefaultAsync(f => f.Id == request.RideId, cancellationToken) ?? throw new Exception("no_ride_found");

      ride.DriverId = request.DriverId;
      ride.Status = RideStatus.DriverAssigned;
      ride.LastUpdateDate = DateTime.UtcNow;

      await db.SaveChangesAsync(cancellationToken);

      await hub.Clients.Group($"ride_{ride.Id}").SendToRiderRideAccepted(ride.Id);
    }
  }
}
