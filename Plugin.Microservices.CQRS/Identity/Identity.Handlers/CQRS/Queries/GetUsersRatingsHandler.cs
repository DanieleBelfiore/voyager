using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Identity.Core.CQRS.Queries;
using Identity.Handlers.Interfaces;
using Hikyaku;
using Microsoft.EntityFrameworkCore;

namespace Identity.Handlers.CQRS.Queries;

public class GetUsersRatingsHandler(IIdentityContext db) : IRequestHandler<GetUsersRatings, Dictionary<Guid, double>>
{
  // Only users who have actually been rated are returned. Ratings defaults to 0.0, so including
  // an unrated user put them in the dictionary with the worst possible score — which meant
  // SearchBestDriver's "no ratings yet defaults to the midpoint" fallback (GetValueOrDefault)
  // never fired for anyone registered, and every new driver ranked last forever.
  public async Task<Dictionary<Guid, double>> Handle(GetUsersRatings request, CancellationToken cancellationToken)
  {
    return await db.Users.AsNoTracking().Where(f => request.UserIds.Contains(f.Id) && f.RatingsCount > 0)
      .ToDictionaryAsync(k => k.Id, v => v.Ratings, cancellationToken);
  }
}
