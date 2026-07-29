using System.Threading;
using System.Threading.Tasks;
using Driver.Application.Ports;
using MediatR;
using Voyager.Contracts.Driver;

namespace Driver.Application.CQRS.Commands;

public class AddDriverHandler(IDriverRepository repository) : IRequestHandler<AddDriver>
{
  public async Task Handle(AddDriver request, CancellationToken cancellationToken)
  {
    var driver = await repository.GetByIdAsync(request.DriverId, cancellationToken);
    if (driver != null)
      return;

    repository.Add(new Domain.Entities.Driver(request.DriverId));

    await repository.SaveChangesAsync(cancellationToken);
  }
}
