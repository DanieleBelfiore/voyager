using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Driver.Api.Persistence;
using Hikyaku;
using Microsoft.EntityFrameworkCore;
using Voyager.Shared.Cache;

namespace Driver.Api.Features.GetDriverStatus;

public class GetDriverStatusHandler(DriverDbContext db, ICacheService cache) : IRequestHandler<GetDriverStatus, DriverStatusResponse>
{
  private const string CacheKeyPrefix = "driver:status:";
  private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

  public async Task<DriverStatusResponse> Handle(GetDriverStatus request, CancellationToken cancellationToken)
  {
    // Live GPS is self-read only. A rider tracking their assigned driver goes through Ride's
    // /eta, which checks ride participation, or Hub's push — answering them here instead would
    // need Driver to query Ride and invert the existing Ride -> Driver dependency. Checked before
    // the cache so a foreign caller cannot be served from a warm entry.
    if (request.Id != request.CallerId)
      throw new UnauthorizedAccessException("not_driver_owner");

    var cacheKey = $"{CacheKeyPrefix}{request.Id}";

    return await cache.GetOrCreateAsync(cacheKey, async () =>
    {
      var driver = await db.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
        ?? throw new KeyNotFoundException("driver_not_found");

      return new DriverStatusResponse
      {
        Id = driver.Id,
        Status = driver.Status,
        LastLocation = driver.LastLocation,
        LastUpdateDate = driver.LastUpdateDate
      };
    }, CacheExpiration);
  }
}
