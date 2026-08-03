using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Core.Cache;
using Common.Core.Exceptions;
using Common.Core.Validation;
using Driver.Core.CQRS.Commands;
using Driver.Handlers.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.IO;

namespace Driver.Handlers.CQRS.Commands;

public class UpdateLocationHandler(IDriverContext db, ICacheService cache) : IRequestHandler<UpdateLocation>
{
  private const string CacheKeyPrefix = "driver:status:";

  public async Task Handle(UpdateLocation request, CancellationToken cancellationToken)
  {
    // Also reached over SignalR (VoyagerHub.UpdateDriverLocation), which has no model binding or
    // validation filter of its own — the guard has to live here to cover both entry points.
    GeoGuard.Required(request.Location, "location");

    var driver = await db.Drivers.FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken) ?? throw new NotFoundException("driver_not_found");

    driver.LastLocation = request.Location;
    driver.LastLocationGeoJSON = new WKTWriter().Write(driver.LastLocation);
    driver.LastUpdateDate = DateTime.UtcNow;

    await db.SaveChangesAsync(cancellationToken);

    // Invalidate cache
    await cache.RemoveAsync($"{CacheKeyPrefix}{request.Id}");
  }
}
