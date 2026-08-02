using System.Threading;
using System.Threading.Tasks;
using Driver.Module.Entities;
using Driver.Module.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Voyager.Contracts.Driver;

namespace Driver.Module.Features.GetDriverAvailability;

/// <summary>
/// Answers the shared Voyager.Contracts.Driver.GetDriverAvailability contract that Ride sends
/// before creating a ride. Deliberately reports "does not exist" rather than throwing: an
/// unknown DriverId here is a bad client request for Ride to reject, not a fault in this service.
/// </summary>
/// <remarks>No controller: this use case is driven exclusively by the Ride module.</remarks>
internal class GetDriverAvailabilityHandler(DriverDbContext db) : IRequestHandler<Voyager.Contracts.Driver.GetDriverAvailability, DriverAvailabilityInfo>
{
  public async Task<DriverAvailabilityInfo> Handle(Voyager.Contracts.Driver.GetDriverAvailability request, CancellationToken cancellationToken)
  {
    var driver = await db.Drivers.AsNoTracking()
      .FirstOrDefaultAsync(d => d.Id == request.DriverId, cancellationToken);

    return new DriverAvailabilityInfo
    {
      Exists = driver != null,
      IsAvailable = driver != null && driver.Status == DriverStatus.Available
    };
  }
}
