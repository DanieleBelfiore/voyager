using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RideEntity = Ride.Core.Domain.Ride;

namespace Ride.Core.Ports.Secondary;

public interface IRideRepository
{
  Task<RideEntity> GetByIdAsync(Guid id, CancellationToken cancellationToken);
  Task<RideEntity> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken);
  Task<bool> HasInFlightRideAsync(Guid userId, CancellationToken cancellationToken);
  Task<RideEntity> GetActiveRideAsync(Guid? driverId, Guid? userId, CancellationToken cancellationToken);
  Task<List<RideEntity>> GetDriverHistoryAsync(Guid driverId, int take, int page, CancellationToken cancellationToken);
  Task<List<RideEntity>> GetUserHistoryAsync(Guid userId, int take, int page, CancellationToken cancellationToken);
  void Add(RideEntity ride);
  Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
