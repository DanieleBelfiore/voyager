using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ride.Core.CQRS.Queries;
using Ride.Core.Dtos;
using Ride.Core.Enums;
using Ride.Handlers.Interfaces;

namespace Ride.Handlers.CQRS.Queries;

public class GetRideHistoryHandler(IRideContext db, RideMapper mapper) : IRequestHandler<GetRideHistory, List<RideDetailsResponse>>
{
  public async Task<List<RideDetailsResponse>> Handle(GetRideHistory request, CancellationToken cancellationToken)
  {
    // Clamped here rather than trusted from the caller: take<=0 defaulted to 25 (never
    // unbounded), take capped at 100, page floored at 0 (a negative page would otherwise
    // produce a negative Skip() and throw at the database).
    var take = request.Take <= 0 ? 25 : Math.Min(request.Take, 100);
    var page = Math.Max(request.Page, 0);

    return await mapper.ProjectToRideDetails(db.Rides.AsNoTracking().Where(f => f.UserId == request.UserId && f.Status == RideStatus.Completed)
      .OrderByDescending(f => f.RequestedAt)
      .Skip(take * page)
      .Take(take))
      .ToListAsync(cancellationToken);
  }
}
