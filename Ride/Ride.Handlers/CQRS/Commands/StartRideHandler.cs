using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.IO;
using Ride.Core.CQRS.Commands;
using Ride.Core.Enums;
using Ride.Handlers.Interfaces;

namespace Ride.Handlers.CQRS.Commands
{
  public class StartRideHandler(IRideContext db) : IRequestHandler<StartRide>
  {
    public async Task Handle(StartRide request, CancellationToken cancellationToken)
    {
      var ride = await db.Rides.FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken) ?? throw new Exception("no_ride_found");

      ride.Status = RideStatus.InProgress;
      ride.PickupLocation = request.Location;
      ride.PickupLocationGeoJSON = new WKTWriter().Write(ride.PickupLocation);
      ride.LastLocation = ride.PickupLocation;
      ride.LastLocationGeoJSON = new WKTWriter().Write(ride.LastLocation);
      ride.LastUpdateDate = DateTime.UtcNow;
      ride.StartAt = ride.LastUpdateDate;

      await db.SaveChangesAsync(cancellationToken);
    }
  }
}
