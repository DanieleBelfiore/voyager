using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Driver.Application.Ports;
using MediatR;
using Voyager.Contracts.Driver;

namespace Driver.Application.CQRS.Queries;

/// <summary>
/// Answers the shared Voyager.Contracts.Driver.GetDriverAvailability contract that Ride sends
/// before creating a ride. Deliberately reports "does not exist" rather than throwing: an
/// unknown DriverId here is a bad client request for Ride to reject, not a fault in this service.
/// </summary>
public class GetDriverAvailabilityHandler(IDriverRepository repository) : IRequestHandler<GetDriverAvailability, DriverAvailabilityInfo>
{
  public async Task<DriverAvailabilityInfo> Handle(GetDriverAvailability request, CancellationToken cancellationToken)
  {
    var driver = await repository.GetByIdAsync(request.DriverId, cancellationToken);

    return new DriverAvailabilityInfo
    {
      Exists = driver != null,
      IsAvailable = driver != null && driver.Status == Domain.Enums.DriverStatus.Available
    };
  }
}
