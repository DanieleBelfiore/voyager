using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Driver.Core.Ports.Secondary;
using MediatR;
using Voyager.Contracts.Identity;

namespace Driver.Adapters.Secondary.Messaging;

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
