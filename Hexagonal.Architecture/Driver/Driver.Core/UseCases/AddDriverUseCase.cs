using System.Threading;
using System.Threading.Tasks;
using Driver.Core.Ports.Primary;
using Driver.Core.Ports.Secondary;
using Voyager.Contracts.Driver;
using DriverEntity = Driver.Core.Domain.Driver;

namespace Driver.Core.UseCases;

public class AddDriverUseCase(IDriverRepository repository) : IAddDriverUseCase
{
  public async Task Handle(AddDriver request, CancellationToken cancellationToken)
  {
    var driver = await repository.GetByIdAsync(request.DriverId, cancellationToken);
    if (driver != null)
      return;

    repository.Add(new DriverEntity(request.DriverId));

    await repository.SaveChangesAsync(cancellationToken);
  }
}
