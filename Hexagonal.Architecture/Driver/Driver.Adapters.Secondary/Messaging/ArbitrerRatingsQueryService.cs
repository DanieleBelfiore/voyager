using System;
using System.Collections.Generic;
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
    return await mediator.Send(new GetUsersRatings { UserIds = userIds }, cancellationToken) ?? [];
  }
}
