using System.Threading;
using System.Threading.Tasks;
using Ride.Application.Dtos;
using Ride.Application.Mapping;
using Ride.Application.Ports;
using Hikyaku;

namespace Ride.Application.CQRS.Queries;

public class GetActiveRideHandler(IRideRepository repository, RideMapper mapper) : IRequestHandler<GetActiveRide, ActiveRideResponse>
{
  public async Task<ActiveRideResponse> Handle(GetActiveRide request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetActiveRideAsync(request.DriverId, request.UserId, cancellationToken);

    return ride == null ? null : mapper.ToActiveRide(ride);
  }
}
