using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Identity.Module.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Identity.Module.Features.GetUsersRatings;

internal class GetUsersRatingsHandler(IdentityDbContext db) : IRequestHandler<Voyager.Contracts.Identity.GetUsersRatings, Dictionary<Guid, double>>
{
  public async Task<Dictionary<Guid, double>> Handle(Voyager.Contracts.Identity.GetUsersRatings request, CancellationToken cancellationToken)
  {
    // Only users who have actually been rated are returned. Ratings defaults to 0.0, so including
    // an unrated user put them in the dictionary with the worst possible score — which meant
    // SearchBestDriver's "no ratings yet defaults to the midpoint" fallback (GetValueOrDefault)
    // never fired for anyone registered, and every new driver ranked last forever.
    return await db.Users.AsNoTracking().Where(u => request.UserIds.Contains(u.Id) && u.RatingsCount > 0)
      .ToDictionaryAsync(k => k.Id, v => v.Ratings, cancellationToken);
  }
}
