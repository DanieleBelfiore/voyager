using System;
using System.Collections.Generic;
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
  public class CancelRideHandler(IRideContext db, IHubContext<VoyagerHub, IVoyagerShareClient> hub) : IRequestHandler<CancelRide>
  {
    public async Task Handle(CancelRide request, CancellationToken cancellationToken)
    {
      var ride = await db.Rides.FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken) ?? throw new Exception("no_ride_found");

      var status = new List<RideStatus> { RideStatus.Requested, RideStatus.DriverAssigned };
      if (!status.Contains(ride.Status))
        throw new Exception("operation_not_permitted");

      ride.Status = RideStatus.Cancelled;
      ride.CancellationReason = request.CancellationReason;
      ride.LastUpdateDate = DateTime.UtcNow;
      ride.EndAt = ride.LastUpdateDate;

      await db.SaveChangesAsync(cancellationToken);

      await hub.Clients.Group($"ride_{ride.Id}").SendToDriverRideCancel(ride.Id);
    }
  }
}
