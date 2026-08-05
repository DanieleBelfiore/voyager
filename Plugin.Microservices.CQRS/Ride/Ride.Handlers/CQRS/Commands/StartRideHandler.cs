using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Core.Exceptions;
using Common.Core.Validation;
using Hikyaku;
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
    // Where the driver says they are is recorded on LastLocation only. It used to overwrite
    // PickupLocation, which is the point the rider agreed to and the endpoint the fare is
    // measured from — letting a driver stretch the priced segment before the trip even began.
    ride.LastLocation = request.Location;
    ride.LastLocationGeoJSON = new WKTWriter().Write(ride.LastLocation);
    ride.LastUpdateDate = DateTime.UtcNow;
    ride.StartAt = ride.LastUpdateDate;

    await db.SaveChangesAsync(cancellationToken);
  }
}
