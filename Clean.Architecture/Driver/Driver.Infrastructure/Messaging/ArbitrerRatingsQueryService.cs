using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Driver.Application.Ports;
using MediatR;
using Voyager.Contracts.Identity;

namespace Driver.Infrastructure.Messaging;

/// <summary>
/// Sends the shared GetUsersRatings wire contract through the local MediatR pipeline. No
/// handler for it is registered in the Driver service itself, so Arbitrer's implicit-remote
/// behaviour routes it over RabbitMQ to whichever service does register one (Identity).
/// </summary>
public class ArbitrerRatingsQueryService(IMediator mediator) : IRatingsQueryService
{
  public async Task<Dictionary<Guid, double>> GetRatingsAsync(List<Guid> userIds, CancellationToken cancellationToken)
  {
    var tasks = userIds.Select(async id =>
    {
      var ratings = await mediator.Send(new GetUsersRatings { UserIds = [id] }, cancellationToken);
      return (id, ratings?.FirstOrDefault().Value ?? 0.0);
    });

    var results = await Task.WhenAll(tasks);

    return results.ToDictionary(x => x.id, x => x.Item2);
  }
}
