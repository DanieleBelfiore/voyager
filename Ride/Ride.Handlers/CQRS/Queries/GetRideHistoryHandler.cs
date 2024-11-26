using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Common.Core;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ride.Core.CQRS.Queries;
using Ride.Core.Dtos;
using Ride.Core.Enums;
using Ride.Handlers.Interfaces;

namespace Ride.Handlers.CQRS.Queries
{
  public class GetRideHistoryHandler(IRideContext db, IMapper mapper) : IRequestHandler<GetRideHistory, List<RideDetailsResponse>>
  {
    public async Task<List<RideDetailsResponse>> Handle(GetRideHistory request, CancellationToken cancellationToken)
    {
      return await db.Rides.AsNoTracking().Where(f => f.UserId == request.UserId && f.Status == RideStatus.Completed)
                                          .OrderByDescending(f => f.RequestedAt)
                                          .Skip(request.Take * request.Page)
                                          .TakeIfPositive(request.Take)
                                          .ProjectTo<RideDetailsResponse>(mapper.ConfigurationProvider)
                                          .ToListAsync(cancellationToken);
    }
  }
}
