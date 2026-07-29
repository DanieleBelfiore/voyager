using System;
using System.Threading;
using System.Threading.Tasks;
using Driver.Api.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Voyager.Shared.Cache;

namespace Driver.Api.Features.UpdateLocation;

/// <summary>Handles the shared <see cref="Voyager.Contracts.Driver.UpdateLocation"/> contract directly.</summary>
public class UpdateLocationHandler(DriverDbContext db, ICacheService cache) : IRequestHandler<Voyager.Contracts.Driver.UpdateLocation>
{
  private const string CacheKeyPrefix = "driver:status:";

  public async Task Handle(Voyager.Contracts.Driver.UpdateLocation request, CancellationToken cancellationToken)
  {
    var driver = await db.Drivers.FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
      ?? throw new Exception("driver_not_found");

    driver.UpdateLocation(request.Location);

    await db.SaveChangesAsync(cancellationToken);

    await cache.RemoveAsync($"{CacheKeyPrefix}{request.Id}");
  }
}
