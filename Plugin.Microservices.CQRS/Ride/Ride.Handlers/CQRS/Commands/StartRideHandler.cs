using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Core.Exceptions;
using Common.Core.Validation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.IO;
using Ride.Core.CQRS.Commands;
using Ride.Core.Enums;
using Ride.Handlers.Interfaces;

namespace Ride.Handlers.CQRS.Commands;

public class StartRideHandler(IRideContext db) : IRequestHandler<StartRide>
{
  public async Task Handle(StartRide request, CancellationToken cancellationToken)
  {
    GeoGuard.Required(request.Location, "location");

    var ride = await db.Rides.FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken) ?? throw new NotFoundException("no_ride_found");

    if (ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    if (ride.Status != RideStatus.DriverAssigned)
      throw new ConflictException("operation_not_permitted");

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
