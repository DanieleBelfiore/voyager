using System.Threading;
using System.Threading.Tasks;
using Driver.Core.Ports.Primary;
using Driver.Core.Ports.Secondary;
using Voyager.Contracts.Driver;

namespace Driver.Core.UseCases;

/// <summary>
/// Answers the shared Voyager.Contracts.Driver.GetDriverAvailability contract that Ride sends
/// before creating a ride. Deliberately reports "does not exist" rather than throwing: an
/// unknown DriverId here is a bad client request for Ride to reject, not a fault in this service.
/// </summary>
public class GetDriverAvailabilityUseCase(IDriverRepository repository) : IGetDriverAvailabilityUseCase
{
  public async Task<DriverAvailabilityInfo> Handle(GetDriverAvailability request, CancellationToken cancellationToken)
  {
    var driver = await repository.GetByIdAsync(request.DriverId, cancellationToken);

    return new DriverAvailabilityInfo
    {
      Exists = driver != null,
      IsAvailable = driver != null && driver.Status == Core.Domain.DriverStatus.Available
    };
  }
}
