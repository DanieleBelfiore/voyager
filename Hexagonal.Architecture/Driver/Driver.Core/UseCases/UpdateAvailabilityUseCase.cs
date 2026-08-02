using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Driver.Core.Ports.Primary;
using Driver.Core.Ports.Secondary;

namespace Driver.Core.UseCases;

public class UpdateAvailabilityUseCase(IDriverRepository repository) : IUpdateAvailabilityUseCase
{
  public async Task Handle(UpdateAvailability request, CancellationToken cancellationToken)
  {
    var driver = await repository.GetByIdAsync(request.Id, cancellationToken) ?? throw new KeyNotFoundException("driver_not_found");

    driver.UpdateAvailability(request.Status);

    await repository.SaveChangesAsync(cancellationToken);
  }
}
