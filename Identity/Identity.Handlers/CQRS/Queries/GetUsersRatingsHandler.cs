using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Identity.Core.CQRS.Queries;
using Identity.Handlers.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Ride.Handlers.CQRS.Queries
{
  public class GetUsersRatingsHandler(IIdentityContext db) : IRequestHandler<GetUsersRatings, Dictionary<Guid, double>>
  {
    public async Task<Dictionary<Guid, double>> Handle(GetUsersRatings request, CancellationToken cancellationToken)
    {
      return await db.Users.AsNoTracking().Where(f => request.UserIds.Contains(f.Id))
                                          .ToDictionaryAsync(k => k.Id, v => v.Ratings, cancellationToken);
    }
  }
}
