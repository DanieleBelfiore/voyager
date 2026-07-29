using System;
using System.Threading;
using System.Threading.Tasks;
using Driver.Core.Ports.Primary;
using Driver.Core.Ports.Secondary;
using Voyager.Contracts.Driver;

namespace Driver.Core.UseCases;

public class UpdateLocationUseCase(IDriverRepository repository, ICacheService cache) : IUpdateLocationUseCase
{
  private const string CacheKeyPrefix = "driver:status:";

  public async Task Handle(UpdateLocation request, CancellationToken cancellationToken)
  {
    var driver = await repository.GetByIdAsync(request.Id, cancellationToken) ?? throw new Exception("driver_not_found");

    driver.UpdateLocation(request.Location);

    await repository.SaveChangesAsync(cancellationToken);

    await cache.RemoveAsync($"{CacheKeyPrefix}{request.Id}");
  }
}
