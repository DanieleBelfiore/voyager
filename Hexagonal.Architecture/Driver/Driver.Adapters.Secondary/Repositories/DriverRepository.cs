using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Driver.Adapters.Secondary.Persistence;
using Driver.Core.Ports.Secondary;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using DriverEntity = Driver.Core.Domain.Driver;

namespace Driver.Adapters.Secondary.Repositories;

public class DriverRepository(DriverDbContext db) : IDriverRepository
{
  public async Task<DriverEntity> GetByIdAsync(Guid id, CancellationToken cancellationToken)
  {
    return await db.Drivers.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
  }

  // Point.Distance() against a geography-typed column translates to SQL Server's STDistance —
  // real geodetic meters, not the planar/Cartesian distance NTS computes in memory — so this is
  // both the exact cutoff and the prefilter in one query, pushed entirely into SQL. Backed by a
  // spatial index on LastLocation (see the AddDriverLastLocationSpatialIndex migration) so the
  // query optimizer isn't forced into a full table scan.
  public async Task<List<NearbyDriver>> GetAvailableWithinDistanceAsync(
    Point center, double radiusMeters, int maxCandidates, CancellationToken cancellationToken)
  {
    return await db.Drivers.AsNoTracking()
      .Where(f => f.Status == Core.Domain.DriverStatus.Available && f.LastLocation != null
        && f.LastLocation.Distance(center) <= radiusMeters)
      .Select(f => new NearbyDriver { Driver = f, DistanceInMeters = f.LastLocation!.Distance(center) })
      // Nearest-first then capped: an unbounded threshold otherwise materialises every
      // available driver and feeds all their ids into GetUsersRatings as one IN (...).
      .OrderBy(f => f.DistanceInMeters)
      .Take(maxCandidates)
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
