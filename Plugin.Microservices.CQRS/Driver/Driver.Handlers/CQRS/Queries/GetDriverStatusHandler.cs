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
    var cacheKey = $"{CacheKeyPrefix}{request.Id}";

    return await cache.GetOrCreateAsync(cacheKey, async () =>
    {
      return await mapper.ProjectToDto(db.Drivers.AsNoTracking().Where(f => f.Id == request.Id)).FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundException("driver_not_found");
    }, CacheExpiration);
  }
}
