using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Identity.Api.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Features.GetUsersRatings;

/// <summary>Handles the shared <see cref="Voyager.Contracts.Identity.GetUsersRatings"/> contract — remote-only, no local endpoint.</summary>
public class GetUsersRatingsHandler(IdentityDbContext db) : IRequestHandler<Voyager.Contracts.Identity.GetUsersRatings, Dictionary<Guid, double>>
{
  public async Task<Dictionary<Guid, double>> Handle(Voyager.Contracts.Identity.GetUsersRatings request, CancellationToken cancellationToken)
  {
    return await db.Users.AsNoTracking().Where(u => request.UserIds.Contains(u.Id))
      .ToDictionaryAsync(k => k.Id, v => v.Ratings, cancellationToken);
  }
}
