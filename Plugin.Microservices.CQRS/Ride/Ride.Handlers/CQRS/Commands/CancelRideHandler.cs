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

    ride.Status = RideStatus.Cancelled;
    ride.CancellationReason = request.CancellationReason;
    ride.LastUpdateDate = DateTime.UtcNow;
    ride.EndAt = ride.LastUpdateDate;

    await db.SaveChangesAsync(cancellationToken);

    // Always Available, whether or not Accept had already moved them to OnRide — a no-op
    // status write is cheaper than branching on ride.Status to decide if it's needed.
    await mediator.Send(new UpdateAvailability { Id = ride.DriverId, Status = DriverStatus.Available }, cancellationToken);

    await mediator.Publish(new RideCancelled { RideId = ride.Id }, cancellationToken);
  }
}
