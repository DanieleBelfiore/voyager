using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace Driver.Application.Ports;

public interface IDriverRepository
{
  Task<Domain.Entities.Driver> GetByIdAsync(Guid id, CancellationToken cancellationToken);

  Task<List<NearbyDriver>> GetAvailableWithinDistanceAsync(
    Point center, double radiusMeters, int maxCandidates, CancellationToken cancellationToken);
  void Add(Domain.Entities.Driver driver);
  Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
