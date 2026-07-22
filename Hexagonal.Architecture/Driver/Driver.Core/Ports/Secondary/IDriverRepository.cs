using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DriverEntity = Driver.Core.Domain.Driver;

namespace Driver.Core.Ports.Secondary;

public interface IDriverRepository
{
  Task<DriverEntity> GetByIdAsync(Guid id, CancellationToken cancellationToken);
  Task<List<DriverEntity>> GetAvailableWithLocationAsync(CancellationToken cancellationToken);
  void Add(DriverEntity driver);
  Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
