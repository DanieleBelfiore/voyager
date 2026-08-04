using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Core.Exceptions;
using Driver.Core.CQRS.Commands;
using Driver.Core.Enums;
using Hikyaku;
using Microsoft.EntityFrameworkCore;
using Ride.Core.CQRS.Commands;
using Ride.Core.CQRS.Events;
using Ride.Core.Enums;
using Ride.Handlers.Interfaces;

namespace Ride.Handlers.CQRS.Commands;

public class AcceptRideHandler(IRideContext db, IHikyaku mediator) : IRequestHandler<AcceptRide>
{
  public async Task Handle(AcceptRide request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.FirstOrDefaultAsync(f => f.Id == request.RideId, cancellationToken) ?? throw new NotFoundException("no_ride_found");

    if (ride.DriverId != request.DriverId)
      throw new UnauthorizedAccessException("not_ride_participant");

    if (ride.Status != RideStatus.Requested)
      throw new ConflictException("operation_not_permitted");

    ride.Status = RideStatus.DriverAssigned;
    ride.LastUpdateDate = DateTime.UtcNow;

    await db.SaveChangesAsync(cancellationToken);

    // Otherwise the driver keeps ranking in SearchBestDriver while already committed to a ride.
    await mediator.Send(new UpdateAvailability { Id = ride.DriverId, Status = DriverStatus.OnRide }, cancellationToken);

    await mediator.Publish(new RideAccepted { RideId = ride.Id }, cancellationToken);
  }
}
