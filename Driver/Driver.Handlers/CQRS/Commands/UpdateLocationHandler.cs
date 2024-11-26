using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Core.Cache;
using Driver.Handlers.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.IO;

namespace Driver.Core.CQRS.Commands
{
  public class UpdateLocationHandler(IDriverContext db, ICacheService cache) : IRequestHandler<UpdateLocation>
  {
    private const string CacheKeyPrefix = "driver:status:";

    public async Task Handle(UpdateLocation request, CancellationToken cancellationToken)
    {
      var driver = await db.Drivers.FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken) ?? throw new Exception("driver_not_found");

      driver.LastLocation = request.Location;
      driver.LastLocationGeoJSON = new WKTWriter().Write(driver.LastLocation);
      driver.LastUpdateDate = DateTime.UtcNow;

      await db.SaveChangesAsync(cancellationToken);

      // Invalidate cache
      await cache.RemoveAsync($"{CacheKeyPrefix}{request.Id}");
    }
  }
}
