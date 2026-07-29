using System;
using System.Threading;
using System.Threading.Tasks;
using Ride.Core.Ports.Secondary;
using Ride.Core.Ports.Primary;

namespace Ride.Core.UseCases;

public class StartRideUseCase(IRideRepository repository) : IStartRideUseCase
{
  public async Task Handle(StartRide request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetByIdAsync(request.Id, cancellationToken) ?? throw new Exception("no_ride_found");

    ride.Start(request.Location);

    await repository.SaveChangesAsync(cancellationToken);
  }
}
