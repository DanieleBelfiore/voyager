using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Driver.Application.Ports;
using Driver.Domain.Enums;
using MediatR;
using Voyager.Contracts.Driver;

namespace Driver.Application.CQRS.Commands;

/// <summary>
/// Remote-only handlers for Voyager.Contracts.Driver's availability commands — reachable
/// exclusively via Arbitrer from Ride (Accept/Cancel/Complete), no local controller action.
/// </summary>
public class MarkDriverOnRideHandler(IDriverRepository repository) : IRequestHandler<MarkDriverOnRide>
{
  public async Task Handle(MarkDriverOnRide request, CancellationToken cancellationToken)
  {
    var driver = await repository.GetByIdAsync(request.DriverId, cancellationToken) ?? throw new KeyNotFoundException("driver_not_found");

    driver.UpdateAvailability(DriverStatus.OnRide);

    await repository.SaveChangesAsync(cancellationToken);
  }
}

public class MarkDriverAvailableHandler(IDriverRepository repository) : IRequestHandler<MarkDriverAvailable>
{
  public async Task Handle(MarkDriverAvailable request, CancellationToken cancellationToken)
  {
    var driver = await repository.GetByIdAsync(request.DriverId, cancellationToken) ?? throw new KeyNotFoundException("driver_not_found");

    driver.UpdateAvailability(DriverStatus.Available);

    await repository.SaveChangesAsync(cancellationToken);
  }
}
