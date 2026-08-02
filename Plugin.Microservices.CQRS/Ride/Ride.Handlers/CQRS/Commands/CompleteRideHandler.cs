using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Core.Exceptions;
using Driver.Core.CQRS.Commands;
using Driver.Core.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Ride.Core.CQRS.Commands;
using Ride.Core.CQRS.Events;
using Ride.Core.Enums;
using Ride.Handlers.Interfaces;

namespace Ride.Handlers.CQRS.Commands;

public class CompleteRideHandler(IRideContext db, IMediator mediator, IConfiguration configuration) : IRequestHandler<CompleteRide>
{
  public async Task Handle(CompleteRide request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken) ?? throw new NotFoundException("no_ride_found");

    if (ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    if (ride.Status != RideStatus.InProgress)
      throw new ConflictException("operation_not_permitted");

    // Price is computed here, never trusted from the client — a rider/driver-supplied Price
    // would let either side under- or over-charge the other.
    var distanceInMeters = ride.PickupLocation != null ? RideGeoCalculator.DistanceInMeters(ride.PickupLocation, request.Location) : 0;
    var durationMinutes = ride.StartAt.HasValue ? (DateTime.UtcNow - ride.StartAt.Value).TotalMinutes : 0;
    var price = configuration.GetValue<double>("BaseFare")
      + configuration.GetValue<double>("PerKmRate") * (distanceInMeters / 1000)
      + configuration.GetValue<double>("PerMinuteRate") * durationMinutes;

    ride.Status = RideStatus.Completed;
    ride.DropoffLocation = request.Location;
    ride.LastLocation = ride.DropoffLocation;
    ride.LastUpdateDate = DateTime.UtcNow;
    ride.EndAt = ride.LastUpdateDate;
    ride.Price = Math.Round(price, 2);

    await db.SaveChangesAsync(cancellationToken);

    await mediator.Send(new UpdateAvailability { Id = ride.DriverId, Status = DriverStatus.Available }, cancellationToken);

    await mediator.Publish(new RideCompleted { RideId = ride.Id }, cancellationToken);
  }
}
