using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Driver.Application.Ports;

public interface IDriverRepository
{
  Task<Domain.Entities.Driver> GetByIdAsync(Guid id, CancellationToken cancellationToken);
  Task<List<Domain.Entities.Driver>> GetAvailableWithLocationAsync(CancellationToken cancellationToken);
  void Add(Domain.Entities.Driver driver);
  Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
