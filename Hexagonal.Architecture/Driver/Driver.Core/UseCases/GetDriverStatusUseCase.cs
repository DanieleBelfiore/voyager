using System;
using System.Threading;
using System.Threading.Tasks;
using Driver.Core.Dtos;
using Driver.Core.Mapping;
using Driver.Core.Ports.Primary;
using Driver.Core.Ports.Secondary;

namespace Driver.Core.UseCases;

public class GetDriverStatusUseCase(IDriverRepository repository, DriverMapper mapper, ICacheService cache) : IGetDriverStatusUseCase
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
