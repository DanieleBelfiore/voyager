using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Driver.Module.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Voyager.Shared.Cache;

namespace Driver.Module.Features.UpdateLocation;

internal class UpdateLocationHandler(DriverDbContext db, ICacheService cache) : IRequestHandler<Voyager.Contracts.Driver.UpdateLocation>
{
  private const string CacheKeyPrefix = "driver:status:";

  public async Task Handle(Voyager.Contracts.Driver.UpdateLocation request, CancellationToken cancellationToken)
  {
    var driver = await db.Drivers.FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
      ?? throw new KeyNotFoundException("driver_not_found");

    driver.UpdateLocation(request.Location);

    await db.SaveChangesAsync(cancellationToken);

    await cache.RemoveAsync($"{CacheKeyPrefix}{request.Id}");
  }
}
