using System;
using System.Threading;
using System.Threading.Tasks;
using Driver.Application.Dtos;
using Driver.Application.Mapping;
using Driver.Application.Ports;
using MediatR;

namespace Driver.Application.CQRS.Queries;

public class GetDriverStatusHandler(IDriverRepository repository, DriverMapper mapper, ICacheService cache) : IRequestHandler<GetDriverStatus, DriverStatusResponse>
{
  private const string CacheKeyPrefix = "driver:status:";
  private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

  public async Task<DriverStatusResponse> Handle(GetDriverStatus request, CancellationToken cancellationToken)
  {
    var cacheKey = $"{CacheKeyPrefix}{request.Id}";

    return await cache.GetOrCreateAsync(cacheKey, async () =>
    {
      var driver = await repository.GetByIdAsync(request.Id, cancellationToken) ?? throw new Exception("driver_not_found");

      return mapper.ToDto(driver);
    }, CacheExpiration);
  }
}
