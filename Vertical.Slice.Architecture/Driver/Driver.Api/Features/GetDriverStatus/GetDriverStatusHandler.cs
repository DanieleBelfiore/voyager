using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Driver.Api.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Voyager.Shared.Cache;

namespace Driver.Api.Features.GetDriverStatus;

public class GetDriverStatusHandler(DriverDbContext db, ICacheService cache) : IRequestHandler<GetDriverStatus, DriverStatusResponse>
{
  private const string CacheKeyPrefix = "driver:status:";
  private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

  public async Task<DriverStatusResponse> Handle(GetDriverStatus request, CancellationToken cancellationToken)
  {
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
