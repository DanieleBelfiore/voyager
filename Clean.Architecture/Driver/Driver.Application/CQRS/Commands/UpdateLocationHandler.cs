using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Driver.Application.Ports;
using MediatR;
using Voyager.Contracts.Driver;
using Driver.Application.Validation;

namespace Driver.Application.CQRS.Commands;

public class UpdateLocationHandler(IDriverRepository repository, ICacheService cache) : IRequestHandler<UpdateLocation>
{
  private const string CacheKeyPrefix = "driver:status:";

  public async Task Handle(UpdateLocation request, CancellationToken cancellationToken)
  {
    // Also reached over SignalR, which has no model binding or validation filter of its own —
    // the guard has to live here to cover both entry points.
    GeoGuard.Required(request.Location, "location");

    var driver = await repository.GetByIdAsync(request.Id, cancellationToken) ?? throw new KeyNotFoundException("driver_not_found");

    driver.UpdateLocation(request.Location);

    await repository.SaveChangesAsync(cancellationToken);

    // Invalidate cache
    await cache.RemoveAsync($"{CacheKeyPrefix}{request.Id}");
  }
}
