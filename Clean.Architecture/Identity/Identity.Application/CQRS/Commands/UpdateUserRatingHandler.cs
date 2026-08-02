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
    var user = await repository.GetByIdAsync(request.UserId, cancellationToken) ?? throw new KeyNotFoundException("user_not_found");

    user.UpdateRating(request.Rating);

    await repository.SaveChangesAsync(cancellationToken);

    return user.Ratings;
  }
}
