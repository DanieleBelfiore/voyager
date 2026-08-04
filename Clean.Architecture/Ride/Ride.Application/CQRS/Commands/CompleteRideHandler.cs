using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ride.Application.CQRS.Queries;
using Ride.Application.Ports;
using Hikyaku;
using Ride.Application.Validation;

namespace Ride.Application.CQRS.Commands;

public class CompleteRideHandler(IRideRepository repository, IRideEventPublisher events, IDriverAvailabilityNotifier availability, IFareConfig fareConfig) : IRequestHandler<CompleteRide>
{
  public async Task Handle(CompleteRide request, CancellationToken cancellationToken)
  {
    // Guarded before the fare is computed from it: a null threw inside the distance
    // calculation, and a bogus coordinate silently inflated the price the rider is charged.
    GeoGuard.Required(request.Location, "location");

    var ride = await repository.GetByIdAsync(request.Id, cancellationToken) ?? throw new KeyNotFoundException("no_ride_found");

    if (ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    var distanceInMeters = ride.PickupLocation != null ? RideEtaCalculator.DistanceInMeters(ride.PickupLocation, request.Location) : 0;
    var durationMinutes = ride.StartAt.HasValue ? (DateTime.UtcNow - ride.StartAt.Value).TotalMinutes : 0;
    var price = RideFareCalculator.Calculate(distanceInMeters, durationMinutes, fareConfig);

    ride.Complete(request.Location, price);

    await repository.SaveChangesAsync(cancellationToken);

    await availability.MarkAvailableAsync(ride.DriverId, cancellationToken);

    await events.RideCompletedAsync(ride.Id, cancellationToken);
  }
}
