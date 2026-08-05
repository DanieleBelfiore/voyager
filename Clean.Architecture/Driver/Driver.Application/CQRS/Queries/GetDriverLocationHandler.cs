using System.Threading;
using System.Threading.Tasks;
using Driver.Application.Ports;
using Hikyaku;
using Voyager.Contracts.Driver;

namespace Driver.Application.CQRS.Queries;

/// <summary>
/// Answers the shared Voyager.Contracts.Driver.GetDriverLocation contract that Ride sends for ETA
/// and that Hub sends for its arrival-distance check. Same absence semantics as
/// GetDriverAvailabilityHandler: an unknown DriverId reports a null location for the caller to
/// handle rather than throwing, since it is a bad request upstream and not a fault in this service.
/// </summary>
public class GetDriverLocationHandler(IDriverRepository repository) : IRequestHandler<GetDriverLocation, DriverLocationInfo>
{
  public async Task<DriverLocationInfo> Handle(GetDriverLocation request, CancellationToken cancellationToken)
  {
    var driver = await repository.GetByIdAsync(request.DriverId, cancellationToken);

    return new DriverLocationInfo { LastLocation = driver?.LastLocation };
  }
}
