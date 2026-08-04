using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hikyaku;
using Microsoft.EntityFrameworkCore;
using Ride.Module.Entities;
using Ride.Module.Persistence;
using ActiveRideInfo = Voyager.Contracts.Ride.ActiveRideInfo;
using SharedGetActiveRide = Voyager.Contracts.Ride.GetActiveRide;

namespace Ride.Module.Features.GetActiveRideForHub;

/// <summary>
/// Handles the shared Voyager.Contracts.Ride.GetActiveRide contract — no controller, only ever
/// driven by Hub's module calling IHikyaku.Send in the same process (asking which ride group
/// to notify).
/// </summary>
internal class GetActiveRideForHubHandler(RideDbContext db) : IRequestHandler<SharedGetActiveRide, ActiveRideInfo>
{
  public async Task<ActiveRideInfo> Handle(SharedGetActiveRide request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.AsNoTracking()
      .Where(r => (r.DriverId == request.DriverId || r.UserId == request.UserId) && (r.Status == RideStatus.DriverAssigned || r.Status == RideStatus.InProgress))
      .FirstOrDefaultAsync(cancellationToken);

    return ride == null ? null : new ActiveRideInfo { Id = ride.Id, PickupLocation = ride.PickupLocation, HasStarted = ride.HasStarted() };
  }
}
