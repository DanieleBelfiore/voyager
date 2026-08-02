using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ride.Adapters.Secondary.Persistence;
using Ride.Core.Domain;
using Ride.Core.Ports.Secondary;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RideEntity = Ride.Core.Domain.Ride;
using Voyager.Errors;

namespace Ride.Adapters.Secondary.Repositories;

public class RideRepository(RideDbContext db) : IRideRepository
{
  private static readonly List<RideStatus> InFlightStatuses = [RideStatus.Requested, RideStatus.DriverAssigned, RideStatus.InProgress];
  private static readonly List<RideStatus> ActiveStatuses = [RideStatus.DriverAssigned, RideStatus.InProgress];

  public async Task<RideEntity> GetByIdAsync(Guid id, CancellationToken cancellationToken)
  {
    return await db.Rides.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
  }

  public async Task<RideEntity> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken)
  {
    return await db.Rides.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
  }

  public async Task<bool> HasInFlightRideAsync(Guid userId, CancellationToken cancellationToken)
  {
    return await db.Rides.AsNoTracking().AnyAsync(f => f.UserId == userId && InFlightStatuses.Contains(f.Status), cancellationToken);
  }

  public async Task<RideEntity> GetActiveRideAsync(Guid? driverId, Guid? userId, CancellationToken cancellationToken)
  {
    return await db.Rides.AsNoTracking()
      .Where(f => (f.DriverId == driverId || f.UserId == userId) && ActiveStatuses.Contains(f.Status))
      .FirstOrDefaultAsync(cancellationToken);
  }

  public async Task<List<RideEntity>> GetDriverHistoryAsync(Guid driverId, int take, int page, CancellationToken cancellationToken)
  {
    // Clamped here rather than trusted from the caller: take<=0 defaulted to 25 (never
    // unbounded), take capped at 100, page floored at 0 (a negative page would otherwise
    // produce a negative Skip() and throw at the database).
    take = take <= 0 ? 25 : Math.Min(take, 100);
    page = Math.Max(page, 0);

    return await db.Rides.AsNoTracking().Where(f => f.DriverId == driverId && f.Status == RideStatus.Completed)
      .OrderByDescending(f => f.RequestedAt)
      .Skip(take * page)
      .Take(take)
      .ToListAsync(cancellationToken);
  }

  public async Task<List<RideEntity>> GetUserHistoryAsync(Guid userId, int take, int page, CancellationToken cancellationToken)
  {
    // Clamped here rather than trusted from the caller: take<=0 defaulted to 25 (never
    // unbounded), take capped at 100, page floored at 0 (a negative page would otherwise
    // produce a negative Skip() and throw at the database).
    take = take <= 0 ? 25 : Math.Min(take, 100);
    page = Math.Max(page, 0);

    return await db.Rides.AsNoTracking().Where(f => f.UserId == userId && f.Status == RideStatus.Completed)
      .OrderByDescending(f => f.RequestedAt)
      .Skip(take * page)
      .Take(take)
      .ToListAsync(cancellationToken);
  }

  public void Add(RideEntity ride)
  {
    db.Rides.Add(ride);
  }

  public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
  {
    try
    {
      return await db.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
    {
      // Unique constraint violation on IX_Rides_UserId_ActiveRide: another request won the race
      // to insert the user's active ride between our in-memory check and this insert.
      throw new ConflictException("ride_already_in_progress");
    }
  }
}
