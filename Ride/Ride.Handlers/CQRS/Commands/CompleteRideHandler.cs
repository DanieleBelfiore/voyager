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
  public class CompleteRideHandler(IRideContext db, IHubContext<VoyagerHub, IVoyagerShareClient> hub) : IRequestHandler<CompleteRide>
  {
    public async Task Handle(CompleteRide request, CancellationToken cancellationToken)
    {
      var ride = await db.Rides.FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken) ?? throw new Exception("no_ride_found");

      ride.Status = RideStatus.Completed;
      ride.DropoffLocation = request.Location;
      ride.LastLocation = ride.DropoffLocation;
      ride.LastUpdateDate = DateTime.UtcNow;
      ride.EndAt = ride.LastUpdateDate;
      ride.Price = request.Price;

      await db.SaveChangesAsync(cancellationToken);

      await hub.Clients.Group($"ride_{ride.Id}").SendToRiderRideCompleted(ride.Id);
    }
  }
}
