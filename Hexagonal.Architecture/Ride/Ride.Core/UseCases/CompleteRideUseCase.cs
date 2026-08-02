using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ride.Core.Ports.Secondary;
using Ride.Core.Ports.Primary;

namespace Ride.Core.UseCases;

public class CompleteRideUseCase(IRideRepository repository, IRideEventPublisher events, IDriverAvailabilityNotifier availability, IFareConfig fareConfig) : ICompleteRideUseCase
{
  public async Task Handle(CompleteRide request, CancellationToken cancellationToken)
  {
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
