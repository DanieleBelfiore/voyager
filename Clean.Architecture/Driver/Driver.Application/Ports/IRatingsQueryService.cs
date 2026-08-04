using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Driver.Application.Ports;

/// <summary>
/// Port for the cross-service rating lookup owned by the Identity bounded context.
/// Infrastructure implements this over the message bus (Kaido/RabbitMQ) using the shared
/// wire contract in Voyager.Contracts — Application only knows it can ask for ratings by id.
/// </summary>
public interface IRatingsQueryService
{
  Task<Dictionary<Guid, double>> GetRatingsAsync(List<Guid> userIds, CancellationToken cancellationToken);
}
