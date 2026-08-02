using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ride.Core.Dtos;
using Ride.Core.Mapping;
using Ride.Core.Ports.Secondary;
using Ride.Core.Ports.Primary;
using RideEntity = Ride.Core.Domain.Ride;
using Voyager.Errors;

namespace Ride.Core.UseCases;

public class RequestRideUseCase(IRideRepository repository, RideMapper mapper, IRideEventPublisher events, IDriverAvailabilityQuery driverAvailability) : IRequestRideUseCase
{
  public async Task<RideDetailsResponse> Handle(RequestRide request, CancellationToken cancellationToken)
  {
    // DriverId comes straight off the request body, so it is checked before a ride is created
    // against it — otherwise a rider can pin a ride onto any GUID at all, including one that
    // belongs to a non-driver or to a driver already committed to someone else's trip.
    var availability = await driverAvailability.GetAvailabilityAsync(request.DriverId, cancellationToken);
    if (!availability.Exists)
      throw new KeyNotFoundException("driver_not_found");

    if (!availability.IsAvailable)
      throw new ConflictException("driver_not_available");

    var alreadyRequested = await repository.HasInFlightRideAsync(request.UserId, cancellationToken);
    if (alreadyRequested)
      throw new ConflictException("ride_already_in_progress");

    var ride = new RideEntity(request.UserId, request.DriverId, request.PickupLocation, request.DropoffLocation);

    repository.Add(ride);

    await repository.SaveChangesAsync(cancellationToken);

    await events.NewRideRequestedAsync(ride.Id, ride.DriverId, cancellationToken);

    return mapper.ToRideDetails(ride);
  }
}
