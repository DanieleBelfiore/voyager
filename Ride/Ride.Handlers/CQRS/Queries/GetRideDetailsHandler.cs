using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ride.Core.CQRS.Queries;
using Ride.Core.Dtos;
using Ride.Handlers.Interfaces;

namespace Ride.Handlers.CQRS.Queries
{
  public class GetRideDetailsHandler(IRideContext db, IMapper mapper) : IRequestHandler<GetRideDetails, RideDetailsResponse>
  {
    public async Task<RideDetailsResponse> Handle(GetRideDetails request, CancellationToken cancellationToken)
    {
      var ride = await db.Rides.AsNoTracking().Where(f => f.Id == request.Id)
                                              .ProjectTo<RideDetailsResponse>(mapper.ConfigurationProvider)
                                              .FirstOrDefaultAsync(cancellationToken);

      return ride ?? throw new Exception("ride_not_found");
    }
  }
}
