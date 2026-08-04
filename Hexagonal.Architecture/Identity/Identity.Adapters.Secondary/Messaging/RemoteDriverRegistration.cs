using System;
using System.Threading;
using System.Threading.Tasks;
using Identity.Core.Ports.Secondary;
using Hikyaku;
using Voyager.Contracts.Driver;

namespace Identity.Adapters.Secondary.Messaging;

public class RemoteDriverRegistration(IHikyaku mediator) : IDriverRegistration
{
  public async Task RegisterAsDriverAsync(Guid userId, CancellationToken cancellationToken)
  {
    await mediator.Send(new AddDriver { DriverId = userId }, cancellationToken);
  }
}
