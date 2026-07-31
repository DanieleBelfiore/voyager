using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ride.Application.Ports;

/// <summary>
/// Port for the cross-service rating update owned by the Identity bounded context.
/// </summary>
public interface IRatingUpdateService
{
  Task<double> UpdateRatingAsync(Guid userId, int rating, CancellationToken cancellationToken);
}
