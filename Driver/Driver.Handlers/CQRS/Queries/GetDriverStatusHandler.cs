using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Common.Core.Cache;
using Driver.Core.Dtos;
using Driver.Handlers.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Driver.Core.CQRS.Queries
{
  public class GetDriverStatusHandler(IDriverContext db, IMapper mapper, ICacheService cache) : IRequestHandler<GetDriverStatus, DriverStatusResponse>
  {
    private const string CacheKeyPrefix = "driver:status:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public async Task<DriverStatusResponse> Handle(GetDriverStatus request, CancellationToken cancellationToken)
    {
      var cacheKey = $"{CacheKeyPrefix}{request.Id}";

      return await cache.GetOrCreateAsync(cacheKey, async () =>
      {
        return await db.Drivers.AsNoTracking().Where(f => f.Id == request.Id).ProjectTo<DriverStatusResponse>(mapper.ConfigurationProvider).FirstOrDefaultAsync(cancellationToken) ?? throw new Exception("driver_not_found");
      }, CacheExpiration);
    }
  }
}
