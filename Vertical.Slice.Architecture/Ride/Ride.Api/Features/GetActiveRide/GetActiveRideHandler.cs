using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hikyaku;
using Microsoft.EntityFrameworkCore;
using Ride.Api.Entities;
using Ride.Api.Persistence;

namespace Ride.Api.Features.GetActiveRide;

public class GetActiveRideHandler(RideDbContext db) : IRequestHandler<GetActiveRide, ActiveRideResponse>
{
  public async Task<ActiveRideResponse> Handle(GetActiveRide request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.AsNoTracking()
      .Where(r => (r.DriverId == request.DriverId || r.UserId == request.UserId) && (r.Status == RideStatus.DriverAssigned || r.Status == RideStatus.InProgress))
      .FirstOrDefaultAsync(cancellationToken);

    return ride == null ? null : new ActiveRideResponse
    {
      Id = ride.Id,
      UserId = ride.UserId,
      DriverId = ride.DriverId,
      RequestedAt = ride.RequestedAt,
      StartAt = ride.StartAt,
      EndAt = ride.EndAt,
      Price = ride.Price,
      Status = ride.Status,
      CancellationReason = ride.CancellationReason,
      PickupLocation = ride.PickupLocation,
      DropoffLocation = ride.DropoffLocation,
      LastLocation = ride.LastLocation,
      LastUpdateDate = ride.LastUpdateDate
    };
  }
}
