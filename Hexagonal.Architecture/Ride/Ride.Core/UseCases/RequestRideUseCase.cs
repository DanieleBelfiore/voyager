using System;
using System.Threading;
using System.Threading.Tasks;
using Ride.Core.Dtos;
using Ride.Core.Mapping;
using Ride.Core.Ports.Secondary;
using Ride.Core.Ports.Primary;
using RideEntity = Ride.Core.Domain.Ride;

namespace Ride.Core.UseCases;

public class RequestRideUseCase(IRideRepository repository, RideMapper mapper, IRideEventPublisher events) : IRequestRideUseCase
{
  public async Task<RideDetailsResponse> Handle(RequestRide request, CancellationToken cancellationToken)
  {
    var alreadyRequested = await repository.HasInFlightRideAsync(request.UserId, cancellationToken);
    if (alreadyRequested)
      throw new InvalidOperationException();

    var ride = new RideEntity(request.UserId, request.DriverId, request.PickupLocation, request.DropoffLocation);

    repository.Add(ride);

    await repository.SaveChangesAsync(cancellationToken);

    await events.NewRideRequestedAsync(ride.Id, cancellationToken);

    return mapper.ToRideDetails(ride);
  }
}
