using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Driver.Application.Ports;
using Driver.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using DriverEntity = Driver.Domain.Entities.Driver;

namespace Driver.Infrastructure.Repositories;

public class DriverRepository(DriverDbContext db) : IDriverRepository
{
  public async Task<DriverEntity> GetByIdAsync(Guid id, CancellationToken cancellationToken)
  {
    return await db.Drivers.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
  }

  public async Task<List<DriverEntity>> GetAvailableWithLocationAsync(CancellationToken cancellationToken)
  {
    return await db.Drivers.AsNoTracking()
      .Where(f => f.Status == Domain.Enums.DriverStatus.Available && f.LastLocation != null)
      .ToListAsync(cancellationToken);
  }

  public void Add(DriverEntity driver)
  {
    db.Drivers.Add(driver);
  }

  public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
  {
    return await db.SaveChangesAsync(cancellationToken);
  }
}
