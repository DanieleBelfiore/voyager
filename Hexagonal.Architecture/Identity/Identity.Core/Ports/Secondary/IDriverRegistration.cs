using System;
using System.Threading;
using System.Threading.Tasks;

namespace Identity.Core.Ports.Secondary;

public interface IDriverRegistration
{
  Task RegisterAsDriverAsync(Guid userId, CancellationToken cancellationToken);
}
