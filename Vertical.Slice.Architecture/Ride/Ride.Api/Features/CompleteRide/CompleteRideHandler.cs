using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hikyaku;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ride.Api.Persistence;
using Ride.Api.Shared;
using Voyager.Contracts.Driver;
using Voyager.Contracts.Ride;

namespace Ride.Api.Features.CompleteRide;

public class CompleteRideHandler(RideDbContext db, IHikyaku mediator, IOptions<FareConfig> fareConfig) : IRequestHandler<CompleteRide>
{
  public async Task Handle(CompleteRide request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken) ?? throw new KeyNotFoundException("no_ride_found");

    if (ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    // Priced off the route the rider agreed to at request time, never off request.Location: that
    // coordinate is asserted by the driver, who is the party being paid by the kilometre.
    var distanceInMeters = ride.PickupLocation != null && ride.DropoffLocation != null
      ? RideEtaCalculator.DistanceInMeters(ride.PickupLocation, ride.DropoffLocation)
      : 0;
    var durationMinutes = ride.StartAt.HasValue ? (DateTime.UtcNow - ride.StartAt.Value).TotalMinutes : 0;
    var price = RideFareCalculator.Calculate(distanceInMeters, durationMinutes, fareConfig.Value);

    ride.Complete(request.Location, price);

    await db.SaveChangesAsync(cancellationToken);

    await mediator.Send(new MarkDriverAvailable { DriverId = ride.DriverId }, cancellationToken);

    await mediator.Publish(new RideCompleted { RideId = ride.Id }, cancellationToken);
  }
}
