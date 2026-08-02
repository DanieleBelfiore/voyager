using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Driver.Core.Domain;
using Driver.Core.Ports.Secondary;
using MediatR;
using Voyager.Contracts.Driver;

namespace Driver.Core.UseCases;

/// <summary>
/// Handle the shared Voyager.Contracts.Driver availability commands directly — reachable only
/// via Arbitrer's remote dispatch (Ride, on Accept/Cancel/Complete), not injected by any local
/// primary adapter, so no dedicated primary port interface — same rationale as
/// GetActiveRideForHubUseCase.
/// </summary>
public class MarkDriverOnRideUseCase(IDriverRepository repository) : IRequestHandler<MarkDriverOnRide>
{
  public async Task Handle(MarkDriverOnRide request, CancellationToken cancellationToken)
  {
    var driver = await repository.GetByIdAsync(request.DriverId, cancellationToken) ?? throw new KeyNotFoundException("driver_not_found");

    driver.UpdateAvailability(DriverStatus.OnRide);

    await repository.SaveChangesAsync(cancellationToken);
  }
}

public class MarkDriverAvailableUseCase(IDriverRepository repository) : IRequestHandler<MarkDriverAvailable>
{
  public async Task Handle(MarkDriverAvailable request, CancellationToken cancellationToken)
  {
    var driver = await repository.GetByIdAsync(request.DriverId, cancellationToken) ?? throw new KeyNotFoundException("driver_not_found");

    driver.UpdateAvailability(DriverStatus.Available);

    await repository.SaveChangesAsync(cancellationToken);
  }
}
