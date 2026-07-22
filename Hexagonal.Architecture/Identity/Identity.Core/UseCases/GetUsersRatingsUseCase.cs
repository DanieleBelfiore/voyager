using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Identity.Core.Ports.Secondary;
using Identity.Core.Ports.Primary;
using Voyager.Contracts.Identity;

namespace Identity.Core.UseCases;

public class GetUsersRatingsUseCase(IUserRepository repository) : IGetUsersRatingsUseCase
{
  public async Task<Dictionary<Guid, double>> Handle(GetUsersRatings request, CancellationToken cancellationToken)
  {
    return await repository.GetRatingsAsync(request.UserIds, cancellationToken);
  }
}
