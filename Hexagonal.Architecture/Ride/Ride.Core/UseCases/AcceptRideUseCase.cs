using System;
using System.Threading;
using System.Threading.Tasks;
using Ride.Core.Ports.Secondary;
using Ride.Core.Ports.Primary;

namespace Ride.Core.UseCases;

public class AcceptRideUseCase(IRideRepository repository, IRideEventPublisher events) : IAcceptRideUseCase
{
  public async Task Handle(AcceptRide request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetByIdAsync(request.RideId, cancellationToken) ?? throw new Exception("no_ride_found");

    ride.Accept(request.DriverId);

    await repository.SaveChangesAsync(cancellationToken);

    await events.RideAcceptedAsync(ride.Id, cancellationToken);
  }
}
