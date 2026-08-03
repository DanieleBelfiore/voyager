using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ride.Core.Ports.Secondary;
using Ride.Core.Ports.Primary;
using Ride.Core.Validation;

namespace Ride.Core.UseCases;

public class StartRideUseCase(IRideRepository repository) : IStartRideUseCase
{
  public async Task Handle(StartRide request, CancellationToken cancellationToken)
  {
    GeoGuard.Required(request.Location, "location");

    var ride = await repository.GetByIdAsync(request.Id, cancellationToken) ?? throw new KeyNotFoundException("no_ride_found");

    if (ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    ride.Start(request.Location);

    await repository.SaveChangesAsync(cancellationToken);
  }
}
