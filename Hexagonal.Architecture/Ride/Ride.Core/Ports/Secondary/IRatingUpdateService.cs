using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ride.Core.Ports.Secondary;

public interface IRatingUpdateService
{
  Task<double> UpdateRatingAsync(Guid userId, int rating, int rides, CancellationToken cancellationToken);
}
