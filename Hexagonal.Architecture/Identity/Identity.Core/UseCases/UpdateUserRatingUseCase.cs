using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Identity.Core.Ports.Secondary;
using Identity.Core.Ports.Primary;
using Voyager.Contracts.Identity;

namespace Identity.Core.UseCases;

public class UpdateUserRatingUseCase(IUserRepository repository) : IUpdateUserRatingUseCase
{
  public async Task<double> Handle(UpdateUserRating request, CancellationToken cancellationToken)
  {
    // Deliberately not load-mutate-save: the average is folded in by a single atomic statement
    // (see IUserRepository.ApplyRatingAsync), because two ratings arriving together used to read
    // the same RatingsCount and silently drop one of them.
    return await repository.ApplyRatingAsync(request.UserId, request.Rating, cancellationToken)
      ?? throw new KeyNotFoundException("user_not_found");
  }
}
