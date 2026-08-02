using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.IO;
using Ride.Core.CQRS.Commands;
using Ride.Core.Enums;
using Ride.Handlers.Interfaces;

namespace Ride.Handlers.CQRS.Commands;

/// <summary>
/// Records the driver's current position on their active ride. Hub sends this on every position
/// report so the ride's own LastLocation tracks the trip — written only at Start and Complete
/// before, which left GET /rides/{id}/location pinned to the pickup point for the whole trip.
/// </summary>
public class TrackRideLocationHandler(IRideContext db) : IRequestHandler<TrackRideLocation>
{
  private static readonly List<RideStatus> ActiveStatuses = [RideStatus.DriverAssigned, RideStatus.InProgress];

  public async Task Handle(TrackRideLocation request, CancellationToken cancellationToken)
  {
    // A position for a ride that vanished (or finished) is not an error worth failing the
    // driver's SignalR call over — the next report supersedes it.
    var ride = await db.Rides.FirstOrDefaultAsync(f => f.Id == request.RideId, cancellationToken);
    if (ride == null || !ActiveStatuses.Contains(ride.Status))
      return;

    ride.LastLocation = request.Location;
    ride.LastLocationGeoJSON = new WKTWriter().Write(ride.LastLocation);
    ride.LastUpdateDate = DateTime.UtcNow;

    await db.SaveChangesAsync(cancellationToken);
  }
}
