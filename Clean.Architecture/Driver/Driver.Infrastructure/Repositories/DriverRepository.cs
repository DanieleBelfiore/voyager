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

  // Bounding-box prefilter pushed into SQL (plain X/Y comparisons, no spatial-function
  // translation involved) instead of pulling every available driver into memory: this is what
  // the geography index on LastLocation actually earns its keep on, and the precise
  // great-circle cutoff still happens in the handler on this already-narrowed set.
  public async Task<List<DriverEntity>> GetAvailableWithinBoundingBoxAsync(
    double minLatitude, double maxLatitude, double minLongitude, double maxLongitude, CancellationToken cancellationToken)
  {
    return await db.Drivers.AsNoTracking()
      .Where(f => f.Status == Domain.Enums.DriverStatus.Available && f.LastLocation != null
        && f.LastLocation.Y >= minLatitude && f.LastLocation.Y <= maxLatitude
        && f.LastLocation.X >= minLongitude && f.LastLocation.X <= maxLongitude)
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
