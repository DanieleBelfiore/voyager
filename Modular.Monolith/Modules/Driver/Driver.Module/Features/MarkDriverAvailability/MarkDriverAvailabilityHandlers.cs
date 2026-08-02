using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Driver.Module.Entities;
using Driver.Module.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Voyager.Contracts.Driver;

namespace Driver.Module.Features.MarkDriverAvailability;

/// <summary>
/// Handles the shared Voyager.Contracts.Driver availability commands directly. No remote hop
/// here (single process) — Ride's module calls IMediator.Send(new MarkDriverOnRide{...}) on
/// Accept/Cancel/Complete, and this internal handler is the one MediatR resolves.
/// </summary>
internal class MarkDriverOnRideHandler(DriverDbContext db) : IRequestHandler<MarkDriverOnRide>
{
  public async Task Handle(MarkDriverOnRide request, CancellationToken cancellationToken)
  {
    var driver = await db.Drivers.FirstOrDefaultAsync(d => d.Id == request.DriverId, cancellationToken) ?? throw new KeyNotFoundException("driver_not_found");

    driver.UpdateAvailability(DriverStatus.OnRide);

    await db.SaveChangesAsync(cancellationToken);
  }
}

internal class MarkDriverAvailableHandler(DriverDbContext db) : IRequestHandler<MarkDriverAvailable>
{
  public async Task Handle(MarkDriverAvailable request, CancellationToken cancellationToken)
  {
    var driver = await db.Drivers.FirstOrDefaultAsync(d => d.Id == request.DriverId, cancellationToken) ?? throw new KeyNotFoundException("driver_not_found");

    driver.UpdateAvailability(DriverStatus.Available);

    await db.SaveChangesAsync(cancellationToken);
  }
}
