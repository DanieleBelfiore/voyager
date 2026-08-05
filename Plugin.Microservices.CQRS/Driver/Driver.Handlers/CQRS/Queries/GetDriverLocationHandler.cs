using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Driver.Core.CQRS.Queries;
using Driver.Handlers.Interfaces;
using Hikyaku;
using Microsoft.EntityFrameworkCore;

namespace Driver.Handlers.CQRS.Queries;

/// <summary>
/// Answers Ride's ETA calculation. Same absence semantics as GetDriverAvailabilityHandler: an
/// unknown DriverId reports a null location for the caller to handle rather than throwing.
/// </summary>
public class GetDriverLocationHandler(IDriverContext db) : IRequestHandler<GetDriverLocation, DriverLocationInfo>
{
  public async Task<DriverLocationInfo> Handle(GetDriverLocation request, CancellationToken cancellationToken)
  {
    var driver = await db.Drivers.AsNoTracking()
      .Where(f => f.Id == request.DriverId)
      .Select(f => new { f.LastLocation })
      .FirstOrDefaultAsync(cancellationToken);

    return new DriverLocationInfo { LastLocation = driver?.LastLocation };
  }
}
