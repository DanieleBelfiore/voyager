using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Driver.Core.CQRS.Queries;
using Driver.Core.Enums;
using Driver.Handlers.Interfaces;
using Hikyaku;
using Microsoft.EntityFrameworkCore;

namespace Driver.Handlers.CQRS.Queries;

/// <summary>
/// Answers Ride's pre-ride availability check. Reports "does not exist" rather than throwing: an
/// unknown DriverId is a bad client request for Ride to reject, not a fault in this service.
/// </summary>
public class GetDriverAvailabilityHandler(IDriverContext db) : IRequestHandler<GetDriverAvailability, DriverAvailabilityInfo>
{
  public async Task<DriverAvailabilityInfo> Handle(GetDriverAvailability request, CancellationToken cancellationToken)
  {
    var driver = await db.Drivers.AsNoTracking()
      .Where(f => f.Id == request.DriverId)
      .Select(f => new { f.Status })
      .FirstOrDefaultAsync(cancellationToken);

    return new DriverAvailabilityInfo
    {
      Exists = driver != null,
      IsAvailable = driver != null && driver.Status == DriverStatus.Available
    };
  }
}
