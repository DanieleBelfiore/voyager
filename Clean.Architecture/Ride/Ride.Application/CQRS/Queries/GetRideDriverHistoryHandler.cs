using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ride.Application.Dtos;
using Ride.Application.Mapping;
using Ride.Application.Ports;
using Hikyaku;

namespace Ride.Application.CQRS.Queries;

public class GetRideDriverHistoryHandler(IRideRepository repository, RideMapper mapper) : IRequestHandler<GetRideDriverHistory, List<RideDetailsResponse>>
{
  public async Task<List<RideDetailsResponse>> Handle(GetRideDriverHistory request, CancellationToken cancellationToken)
  {
    var rides = await repository.GetDriverHistoryAsync(request.DriverId, request.Take, request.Page, cancellationToken);

    return rides.Select(mapper.ToRideDetails).ToList();
  }
}
