using System;
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
    var user = await repository.GetByIdAsync(request.UserId, cancellationToken) ?? throw new InvalidOperationException("user_not_found");

    user.UpdateRating(request.Rating);

    await repository.SaveChangesAsync(cancellationToken);

    return user.Ratings;
  }
}
