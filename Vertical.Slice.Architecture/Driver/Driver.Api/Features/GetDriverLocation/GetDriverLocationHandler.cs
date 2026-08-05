using System.Threading;
using System.Threading.Tasks;
using Driver.Api.Persistence;
using Hikyaku;
using Microsoft.EntityFrameworkCore;
using Voyager.Contracts.Driver;

namespace Driver.Api.Features.GetDriverLocation;

/// <summary>
/// Answers the shared Voyager.Contracts.Driver.GetDriverLocation contract that Ride sends for ETA
/// and Hub for its arrival-distance check. Same absence semantics as GetDriverAvailability: an
/// unknown DriverId reports a null location for the caller to handle rather than throwing.
/// </summary>
/// <remarks>No controller: this use case is driven exclusively by Ride and Hub over Kaido.</remarks>
public class GetDriverLocationHandler(DriverDbContext db) : IRequestHandler<Voyager.Contracts.Driver.GetDriverLocation, DriverLocationInfo>
{
  public async Task<DriverLocationInfo> Handle(Voyager.Contracts.Driver.GetDriverLocation request, CancellationToken cancellationToken)
  {
    var driver = await db.Drivers.AsNoTracking()
      .FirstOrDefaultAsync(d => d.Id == request.DriverId, cancellationToken);

    return new DriverLocationInfo { LastLocation = driver?.LastLocation };
  }
}
