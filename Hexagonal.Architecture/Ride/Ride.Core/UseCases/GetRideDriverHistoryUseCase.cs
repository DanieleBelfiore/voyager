using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ride.Core.Dtos;
using Ride.Core.Mapping;
using Ride.Core.Ports.Secondary;
using Ride.Core.Ports.Primary;

namespace Ride.Core.UseCases;

public class GetRideDriverHistoryUseCase(IRideRepository repository, RideMapper mapper) : IGetRideDriverHistoryUseCase
{
  public async Task<List<RideDetailsResponse>> Handle(GetRideDriverHistory request, CancellationToken cancellationToken)
  {
    var rides = await repository.GetDriverHistoryAsync(request.DriverId, request.Take, request.Page, cancellationToken);

    return rides.Select(mapper.ToRideDetails).ToList();
  }
}
