using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Core.Cache;
using Common.Core.Exceptions;
using Driver.Core.CQRS.Queries;
using Driver.Core.Dtos;
using Driver.Handlers.Interfaces;
using Hikyaku;
using Microsoft.EntityFrameworkCore;

namespace Driver.Handlers.CQRS.Queries;

public class GetDriverStatusHandler(IDriverContext db, DriverMapper mapper, ICacheService cache) : IRequestHandler<GetDriverStatus, DriverStatusResponse>
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
      return await mapper.ProjectToDto(db.Drivers.AsNoTracking().Where(f => f.Id == request.Id)).FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundException("driver_not_found");
    }, CacheExpiration);
  }
}
