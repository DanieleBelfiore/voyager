using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ride.Application.Dtos;
using Ride.Application.Mapping;
using Ride.Application.Ports;
using MediatR;
using RideEntity = Ride.Domain.Entities.Ride;
using Voyager.Errors;

namespace Ride.Application.CQRS.Commands;

public class RequestRideHandler(IRideRepository repository, RideMapper mapper, IRideEventPublisher events, IDriverAvailabilityQuery driverAvailability) : IRequestHandler<RequestRide, RideDetailsResponse>
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

    // SaveChangesAsync translates a DB-level unique-constraint violation (a race losing to
    // IX_Rides_UserId_ActiveOnly) into the same InvalidOperationException thrown above for the
    // in-memory check, so callers see a consistent error either way. See RideRepository.
    await repository.SaveChangesAsync(cancellationToken);

    await events.NewRideRequestedAsync(ride.Id, ride.DriverId, cancellationToken);

    return mapper.ToRideDetails(ride);
  }
}
