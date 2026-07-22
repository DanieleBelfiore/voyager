using System.Threading;
using System.Threading.Tasks;
using Ride.Core.Dtos;
using Ride.Core.Mapping;
using Ride.Core.Ports.Secondary;
using Ride.Core.Ports.Primary;

namespace Ride.Core.UseCases;

public class GetActiveRideUseCase(IRideRepository repository, RideMapper mapper) : IGetActiveRideUseCase
{
  public async Task<ActiveRideResponse> Handle(GetActiveRide request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetActiveRideAsync(request.DriverId, request.UserId, cancellationToken);

    return ride == null ? null : mapper.ToActiveRide(ride);
  }
}
