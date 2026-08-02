using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ride.Application.Ports;
using Ride.Domain.Enums;
using Ride.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using RideEntity = Ride.Domain.Entities.Ride;
using Voyager.Errors;

namespace Ride.Infrastructure.Repositories;

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
    catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
    {
      // Race with another concurrent RequestRide: the in-memory HasInFlightRideAsync check
      // passed for both callers, but only one insert can satisfy the unique filtered index
      // IX_Rides_UserId_ActiveOnly (RideConfiguration). Translate to the same exception
      // RequestRideHandler already throws for the in-memory check, so callers get a
      // consistent error either way.
      throw new ConflictException("duplicate_active_ride", ex);
    }
  }
}
