using System;
using System.Threading;
using System.Threading.Tasks;
using Ride.Core.Ports.Secondary;
using Ride.Core.Ports.Primary;

namespace Ride.Core.UseCases;

public class CancelRideUseCase(IRideRepository repository, IRideEventPublisher events) : ICancelRideUseCase
{
  public async Task Handle(CancelRide request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetByIdAsync(request.Id, cancellationToken) ?? throw new Exception("no_ride_found");

    ride.Cancel(request.CancellationReason);

    await repository.SaveChangesAsync(cancellationToken);

    await events.RideCancelledAsync(ride.Id, cancellationToken);
  }
}
