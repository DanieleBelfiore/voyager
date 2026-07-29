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
    return await db.Users.AsNoTracking().Where(u => request.UserIds.Contains(u.Id))
      .ToDictionaryAsync(k => k.Id, v => v.Ratings, cancellationToken);
  }
}
