using System;
using System.Threading;
using System.Threading.Tasks;

namespace Identity.Application.Ports;

/// <summary>
/// Notifies the Driver bounded context that a newly registered user opted in as a driver.
/// Infrastructure implements this by sending Voyager.Contracts.Driver.AddDriver over the
/// message bus — Application only knows it needs to announce the fact.
/// </summary>
public interface IDriverRegistration
{
  Task RegisterAsDriverAsync(Guid userId, CancellationToken cancellationToken);
}
