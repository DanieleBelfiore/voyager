using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ride.Api.Entities;
using Ride.Api.Persistence;
using Ride.Api.Shared;

namespace Ride.Api.Features.GetRideDriverHistory;

public class GetRideDriverHistoryHandler(RideDbContext db) : IRequestHandler<GetRideDriverHistory, List<RideDetailsResponse>>
{
  public async Task<List<RideDetailsResponse>> Handle(GetRideDriverHistory request, CancellationToken cancellationToken)
  {
    var query = db.Rides.AsNoTracking().Where(r => r.DriverId == request.DriverId && r.Status == RideStatus.Completed)
      .OrderByDescending(r => r.RequestedAt)
      .Skip(request.Take * request.Page);

    var rides = await (request.Take < 0 ? query : query.Take(request.Take)).ToListAsync(cancellationToken);

    return rides.Select(RideDetailsResponse.From).ToList();
  }
}
