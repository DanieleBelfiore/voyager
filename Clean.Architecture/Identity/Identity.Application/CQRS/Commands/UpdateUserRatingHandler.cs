using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Identity.Application.Ports;
using MediatR;
using Voyager.Contracts.Identity;

namespace Identity.Application.CQRS.Commands;

/// <summary>
/// Handles the shared Voyager.Contracts.Identity.UpdateUserRating contract directly — this
/// service is the remote target Ride's RateDriverHandler/RateRideHandler dispatch to via Arbitrer.
/// </summary>
public class UpdateUserRatingHandler(IUserRepository repository) : IRequestHandler<UpdateUserRating, double>
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
