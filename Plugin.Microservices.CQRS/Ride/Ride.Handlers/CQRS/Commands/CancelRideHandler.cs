using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.Core.Exceptions;
using Driver.Core.CQRS.Commands;
using Driver.Core.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ride.Core.CQRS.Commands;
using Ride.Core.CQRS.Events;
using Ride.Core.Enums;
using Ride.Handlers.Interfaces;

namespace Ride.Handlers.CQRS.Commands;

public class CancelRideHandler(IRideContext db, IMediator mediator) : IRequestHandler<CancelRide>
{
  public async Task Handle(CancelRide request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken) ?? throw new NotFoundException("no_ride_found");

    if (ride.UserId != request.CallerId && ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    var status = new List<RideStatus> { RideStatus.Requested, RideStatus.DriverAssigned };
    if (!status.Contains(ride.Status))
      throw new ConflictException("operation_not_permitted");

    // Only a DriverAssigned ride ever moved the driver to OnRide. A ride still at Requested never
    // touched their status, and nothing constrains how many riders hold a Requested ride against
    // the same available driver — the unique index is per-rider (IX_Rides_UserId_ActiveOnly), not
    // per-driver. Releasing unconditionally let a stale request being cancelled flip a driver who
    // is already mid-trip on someone else's ride back into the matching pool.
    var heldTheDriver = ride.Status == RideStatus.DriverAssigned;

    ride.Status = RideStatus.Cancelled;
    ride.CancellationReason = request.CancellationReason;
    ride.LastUpdateDate = DateTime.UtcNow;
    ride.EndAt = ride.LastUpdateDate;

    await db.SaveChangesAsync(cancellationToken);

    if (heldTheDriver)
      await mediator.Send(new UpdateAvailability { Id = ride.DriverId, Status = DriverStatus.Available }, cancellationToken);

    await mediator.Publish(new RideCancelled { RideId = ride.Id, DriverId = ride.DriverId, UserId = ride.UserId }, cancellationToken);
  }
}
