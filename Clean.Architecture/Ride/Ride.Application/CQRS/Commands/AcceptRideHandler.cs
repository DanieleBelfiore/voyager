using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ride.Application.Ports;
using Hikyaku;

namespace Ride.Application.CQRS.Commands;

public class AcceptRideHandler(IRideRepository repository, IRideEventPublisher events, IDriverAvailabilityNotifier availability) : IRequestHandler<AcceptRide>
{
  public async Task Handle(AcceptRide request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetByIdAsync(request.RideId, cancellationToken) ?? throw new KeyNotFoundException("no_ride_found");

    ride.Accept(request.DriverId);

    await repository.SaveChangesAsync(cancellationToken);

    // Otherwise the driver keeps ranking in driver search while already committed to a ride.
    await availability.MarkOnRideAsync(ride.DriverId, cancellationToken);

    await events.RideAcceptedAsync(ride.Id, cancellationToken);
  }
}
