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

public class GetActiveRideHandler(IRideContext db, RideMapper mapper) : IRequestHandler<GetActiveRide, ActiveRideResponse>
{
  public async Task<ActiveRideResponse> Handle(GetActiveRide request, CancellationToken cancellationToken)
  {
    var status = new List<RideStatus> { RideStatus.DriverAssigned, RideStatus.InProgress };

    return await mapper.ProjectToActiveRide(db.Rides.AsNoTracking().Where(f => (f.DriverId == request.DriverId || f.UserId == request.UserId) && status.Contains(f.Status)))
      .FirstOrDefaultAsync(cancellationToken);
  }
}
