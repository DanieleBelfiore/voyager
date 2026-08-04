using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Core.Cache;
using Common.Core.Exceptions;
using Driver.Core.CQRS.Commands;
using Driver.Handlers.Interfaces;
using Hikyaku;
using Microsoft.EntityFrameworkCore;

namespace Driver.Handlers.CQRS.Commands;

public class UpdateAvailabilityHandler(IDriverContext db, ICacheService cache) : IRequestHandler<UpdateAvailability>
{
  private const string CacheKeyPrefix = "driver:status:";

  public async Task Handle(UpdateAvailability request, CancellationToken cancellationToken)
  {
    var driver = await db.Drivers.FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken) ?? throw new NotFoundException("driver_not_found");

    driver.Status = request.Status;
    driver.LastUpdateDate = DateTime.UtcNow;

    await db.SaveChangesAsync(cancellationToken);

    // Invalidate cache
    await cache.RemoveAsync($"{CacheKeyPrefix}{request.Id}");
  }
}
