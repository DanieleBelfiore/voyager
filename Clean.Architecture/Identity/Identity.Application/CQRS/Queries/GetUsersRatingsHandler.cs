using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Identity.Application.Ports;
using MediatR;
using Voyager.Contracts.Identity;

namespace Identity.Application.CQRS.Queries;

/// <summary>
/// Handles the shared Voyager.Contracts.Identity.GetUsersRatings contract directly — this
/// service is the remote target Driver's SearchBestDriverHandler dispatches to via Arbitrer.
/// </summary>
public class GetUsersRatingsHandler(IUserRepository repository) : IRequestHandler<GetUsersRatings, Dictionary<Guid, double>>
{
  public async Task<Dictionary<Guid, double>> Handle(GetUsersRatings request, CancellationToken cancellationToken)
  {
    return await repository.GetRatingsAsync(request.UserIds, cancellationToken);
  }
}
