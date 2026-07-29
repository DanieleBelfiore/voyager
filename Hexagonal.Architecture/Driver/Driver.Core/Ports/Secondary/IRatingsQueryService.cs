using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Driver.Core.Ports.Secondary;

/// <summary>Secondary port for the cross-service rating lookup owned by Identity.</summary>
public interface IRatingsQueryService
{
  Task<Dictionary<Guid, double>> GetRatingsAsync(List<Guid> userIds, CancellationToken cancellationToken);
}
