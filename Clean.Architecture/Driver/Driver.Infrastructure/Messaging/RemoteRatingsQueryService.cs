using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Driver.Application.Ports;
using Hikyaku;
using Voyager.Contracts.Identity;

namespace Driver.Infrastructure.Messaging;

/// <summary>
/// Sends the shared GetUsersRatings wire contract through the local Hikyaku pipeline. No
/// handler for it is registered in the Driver service itself, so Kaido's implicit-remote
/// behaviour routes it over RabbitMQ to whichever service does register one (Identity).
/// </summary>
public class RemoteRatingsQueryService(IHikyaku mediator) : IRatingsQueryService
{
  public async Task<Dictionary<Guid, double>> GetRatingsAsync(List<Guid> userIds, CancellationToken cancellationToken)
  {
    // Returned as-is, without back-filling absent ids. Padding them with 0.0 made every unrated
    // driver look like the worst-rated one and left SearchBestDriverHandler's "no ratings yet
    // defaults to the midpoint" fallback unreachable — an absent id is the signal that fallback
    // exists to read.
    return await mediator.Send(new GetUsersRatings { UserIds = userIds }, cancellationToken) ?? [];
  }
}
