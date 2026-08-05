using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Core.Exceptions;
using Common.Core.Validation;
using Driver.Core.CQRS.Commands;
using Driver.Core.Enums;
using Hikyaku;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Ride.Core.CQRS.Commands;
using Ride.Core.CQRS.Events;
using Ride.Core.Enums;
using Ride.Handlers.Interfaces;

namespace Ride.Handlers.CQRS.Commands;

public class CompleteRideHandler(IRideContext db, IHikyaku mediator, IConfiguration configuration) : IRequestHandler<CompleteRide>
{
  public async Task Handle(CompleteRide request, CancellationToken cancellationToken)
  {
    // Guarded before the fare is computed from it: a null here threw inside the distance
    // calculation, and a bogus coordinate silently inflated the price the rider is charged.
    GeoGuard.Required(request.Location, "location");

    var ride = await db.Rides.FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken) ?? throw new NotFoundException("no_ride_found");

    if (ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    if (ride.Status != RideStatus.InProgress)
      throw new ConflictException("operation_not_permitted");

    // Price is computed here, never trusted from the client — a rider/driver-supplied Price
    // would let either side under- or over-charge the other. The same applies to the distance it
    // is computed from: request.Location is asserted by the driver, who is the party being paid
    // by the kilometre, so the fare is measured over the route the rider agreed to at request time.
    var distanceInMeters = ride.PickupLocation != null && ride.DropoffLocation != null
      ? RideGeoCalculator.DistanceInMeters(ride.PickupLocation, ride.DropoffLocation)
      : 0;
    var durationMinutes = ride.StartAt.HasValue ? (DateTime.UtcNow - ride.StartAt.Value).TotalMinutes : 0;
    var price = configuration.GetValue<double>("BaseFare")
      + configuration.GetValue<double>("PerKmRate") * (distanceInMeters / 1000)
      + configuration.GetValue<double>("PerMinuteRate") * durationMinutes;

    ride.Status = RideStatus.Completed;
    ride.LastLocation = request.Location;
    ride.LastUpdateDate = DateTime.UtcNow;
    ride.EndAt = ride.LastUpdateDate;
    ride.Price = Math.Round(price, 2);

    await db.SaveChangesAsync(cancellationToken);

    await mediator.Send(new UpdateAvailability { Id = ride.DriverId, Status = DriverStatus.Available }, cancellationToken);

    await mediator.Publish(new RideCompleted { RideId = ride.Id }, cancellationToken);
  }
}
