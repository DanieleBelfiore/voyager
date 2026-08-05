using System.Threading;
using System.Threading.Tasks;
using Driver.Core.Ports.Secondary;
using Hikyaku;
using Voyager.Contracts.Driver;

namespace Driver.Core.UseCases;

/// <summary>
/// Handles the shared Voyager.Contracts.Driver.GetDriverLocation contract that Ride sends for ETA
/// and Hub for its arrival-distance check — reachable only via Kaido's remote dispatch, not
/// injected by any local primary adapter, so it has no dedicated primary port interface and
/// implements IRequestHandler&lt;T&gt; itself, which is all Hikyaku's assembly scan needs.
/// Same absence semantics as GetDriverAvailabilityUseCase: an unknown DriverId reports a null
/// location for the caller to handle rather than throwing.
/// </summary>
public class GetDriverLocationUseCase(IDriverRepository repository) : IRequestHandler<GetDriverLocation, DriverLocationInfo>
{
  public async Task<DriverLocationInfo> Handle(GetDriverLocation request, CancellationToken cancellationToken)
  {
    var driver = await repository.GetByIdAsync(request.DriverId, cancellationToken);

    return new DriverLocationInfo { LastLocation = driver?.LastLocation };
  }
}
