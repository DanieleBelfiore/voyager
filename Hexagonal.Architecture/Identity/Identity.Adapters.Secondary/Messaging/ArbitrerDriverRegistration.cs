using System;
using System.Threading;
using System.Threading.Tasks;
using Identity.Core.Ports.Secondary;
using MediatR;
using Voyager.Contracts.Driver;

namespace Identity.Adapters.Secondary.Messaging;

public class ArbitrerDriverRegistration(IMediator mediator) : IDriverRegistration
{
  public async Task RegisterAsDriverAsync(Guid userId, CancellationToken cancellationToken)
  {
    await mediator.Send(new AddDriver { DriverId = userId }, cancellationToken);
  }
}
