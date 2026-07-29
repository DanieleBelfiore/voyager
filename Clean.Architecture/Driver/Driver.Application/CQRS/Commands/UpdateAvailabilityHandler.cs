using System;
using System.Threading;
using System.Threading.Tasks;
using Driver.Application.Ports;
using MediatR;

namespace Driver.Application.CQRS.Commands;

public class UpdateAvailabilityHandler(IDriverRepository repository) : IRequestHandler<UpdateAvailability>
{
  public async Task Handle(UpdateAvailability request, CancellationToken cancellationToken)
  {
    var driver = await repository.GetByIdAsync(request.Id, cancellationToken) ?? throw new Exception("driver_not_found");

    driver.UpdateAvailability(request.Status);

    await repository.SaveChangesAsync(cancellationToken);
  }
}
