using System;
using System.Threading;
using System.Threading.Tasks;
using Ride.Application.Dtos;
using Ride.Application.Mapping;
using Ride.Application.Ports;
using MediatR;
using RideEntity = Ride.Domain.Entities.Ride;

namespace Ride.Application.CQRS.Commands;

public class RequestRideHandler(IRideRepository repository, RideMapper mapper, IRideEventPublisher events) : IRequestHandler<RequestRide, RideDetailsResponse>
{
  public async Task<RideDetailsResponse> Handle(RequestRide request, CancellationToken cancellationToken)
  {
    var alreadyRequested = await repository.HasInFlightRideAsync(request.UserId, cancellationToken);
    if (alreadyRequested)
      throw new InvalidOperationException();

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
