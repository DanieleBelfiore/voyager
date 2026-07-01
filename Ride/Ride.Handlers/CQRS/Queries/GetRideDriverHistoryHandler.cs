using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Core.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ride.Core.CQRS.Queries;
using Ride.Core.Dtos;
using Ride.Core.Enums;
using Ride.Handlers.Interfaces;

namespace Ride.Handlers.CQRS.Queries;

public class GetRideDriverHistoryHandler(IRideContext db, RideMapper mapper) : IRequestHandler<GetRideDriverHistory, List<RideDetailsResponse>>
{
  public async Task<List<RideDetailsResponse>> Handle(GetRideDriverHistory request, CancellationToken cancellationToken)
  {
    return await mapper.ProjectToRideDetails(db.Rides.AsNoTracking().Where(f => f.DriverId == request.DriverId && f.Status == RideStatus.Completed)
      .OrderByDescending(f => f.RequestedAt)
      .Skip(request.Take * request.Page)
      .TakeIfPositive(request.Take))
      .ToListAsync(cancellationToken);
  }
}
