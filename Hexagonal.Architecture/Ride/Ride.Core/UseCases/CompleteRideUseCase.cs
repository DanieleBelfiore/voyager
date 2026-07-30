using System;
using System.Threading;
using System.Threading.Tasks;
using Ride.Core.Ports.Secondary;
using Ride.Core.Ports.Primary;

namespace Ride.Core.UseCases;

public class CompleteRideUseCase(IRideRepository repository, IRideEventPublisher events) : ICompleteRideUseCase
{
  public async Task Handle(CompleteRide request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetByIdAsync(request.Id, cancellationToken) ?? throw new Exception("no_ride_found");

    if (ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    ride.Complete(request.Location, request.Price);

    await repository.SaveChangesAsync(cancellationToken);

    await events.RideCompletedAsync(ride.Id, cancellationToken);
  }
}
