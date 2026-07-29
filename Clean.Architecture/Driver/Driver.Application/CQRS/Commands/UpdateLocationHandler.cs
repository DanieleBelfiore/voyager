using System;
using System.Threading;
using System.Threading.Tasks;
using Driver.Application.Ports;
using MediatR;
using Voyager.Contracts.Driver;

namespace Driver.Application.CQRS.Commands;

public class UpdateLocationHandler(IDriverRepository repository, ICacheService cache) : IRequestHandler<UpdateLocation>
{
  private const string CacheKeyPrefix = "driver:status:";

  public async Task Handle(UpdateLocation request, CancellationToken cancellationToken)
  {
    var driver = await repository.GetByIdAsync(request.Id, cancellationToken) ?? throw new Exception("driver_not_found");

    driver.UpdateLocation(request.Location);

    await repository.SaveChangesAsync(cancellationToken);

    // Invalidate cache
    await cache.RemoveAsync($"{CacheKeyPrefix}{request.Id}");
  }
}
