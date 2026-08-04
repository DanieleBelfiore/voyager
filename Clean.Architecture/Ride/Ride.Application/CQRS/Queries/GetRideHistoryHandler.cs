using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ride.Application.Dtos;
using Ride.Application.Mapping;
using Ride.Application.Ports;
using Hikyaku;

namespace Ride.Application.CQRS.Queries;

public class GetRideHistoryHandler(IRideRepository repository, RideMapper mapper) : IRequestHandler<GetRideHistory, List<RideDetailsResponse>>
{
  public async Task<List<RideDetailsResponse>> Handle(GetRideHistory request, CancellationToken cancellationToken)
  {
    var rides = await repository.GetUserHistoryAsync(request.UserId, request.Take, request.Page, cancellationToken);

    return rides.Select(mapper.ToRideDetails).ToList();
  }
}
