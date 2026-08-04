using System;
using System.Threading;
using System.Threading.Tasks;
using Identity.Application.Ports;
using Hikyaku;
using Voyager.Contracts.Driver;

namespace Identity.Infrastructure.Messaging;

/// <summary>
/// Sends the shared AddDriver wire contract through the local Hikyaku pipeline. No handler for
/// it is registered in the Identity service itself, so Kaido's implicit-remote behaviour
/// routes it over RabbitMQ to the Driver service.
/// </summary>
public class RemoteDriverRegistration(IHikyaku mediator) : IDriverRegistration
{
  public async Task RegisterAsDriverAsync(Guid userId, CancellationToken cancellationToken)
  {
    await mediator.Send(new AddDriver { DriverId = userId }, cancellationToken);
  }
}
