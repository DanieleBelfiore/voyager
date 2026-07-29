using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ride.Core.Dtos;
using Ride.Core.Mapping;
using Ride.Core.Ports.Secondary;
using Ride.Core.Ports.Primary;

namespace Ride.Core.UseCases;

public class GetRideHistoryUseCase(IRideRepository repository, RideMapper mapper) : IGetRideHistoryUseCase
{
  public async Task<List<RideDetailsResponse>> Handle(GetRideHistory request, CancellationToken cancellationToken)
  {
    var rides = await repository.GetUserHistoryAsync(request.UserId, request.Take, request.Page, cancellationToken);

    return rides.Select(mapper.ToRideDetails).ToList();
  }
}
