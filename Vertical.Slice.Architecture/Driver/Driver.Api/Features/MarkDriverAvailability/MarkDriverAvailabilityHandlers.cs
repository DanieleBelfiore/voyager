using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Driver.Api.Entities;
using Driver.Api.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Voyager.Contracts.Driver;

namespace Driver.Api.Features.MarkDriverAvailability;

/// <summary>
/// Handle the shared Voyager.Contracts.Driver availability commands directly — reachable only
/// via Arbitrer's remote dispatch (Ride, on Accept/Cancel/Complete), no local controller.
/// </summary>
public class MarkDriverOnRideHandler(DriverDbContext db) : IRequestHandler<MarkDriverOnRide>
{
  public async Task Handle(MarkDriverOnRide request, CancellationToken cancellationToken)
  {
    var driver = await db.Drivers.FirstOrDefaultAsync(d => d.Id == request.DriverId, cancellationToken) ?? throw new KeyNotFoundException("driver_not_found");

    driver.UpdateAvailability(DriverStatus.OnRide);

    await db.SaveChangesAsync(cancellationToken);
  }
}

public class MarkDriverAvailableHandler(DriverDbContext db) : IRequestHandler<MarkDriverAvailable>
{
  public async Task Handle(MarkDriverAvailable request, CancellationToken cancellationToken)
  {
    var driver = await db.Drivers.FirstOrDefaultAsync(d => d.Id == request.DriverId, cancellationToken) ?? throw new KeyNotFoundException("driver_not_found");

    driver.UpdateAvailability(DriverStatus.Available);

    await db.SaveChangesAsync(cancellationToken);
  }
}
