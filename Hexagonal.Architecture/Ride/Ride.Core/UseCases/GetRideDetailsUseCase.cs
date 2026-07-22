using System;
using System.Threading;
using System.Threading.Tasks;
using Ride.Core.Dtos;
using Ride.Core.Mapping;
using Ride.Core.Ports.Secondary;
using Ride.Core.Ports.Primary;

namespace Ride.Core.UseCases;

public class GetRideDetailsUseCase(IRideRepository repository, RideMapper mapper) : IGetRideDetailsUseCase
{
  public async Task<RideDetailsResponse> Handle(GetRideDetails request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetByIdReadOnlyAsync(request.Id, cancellationToken) ?? throw new Exception("ride_not_found");

    return mapper.ToRideDetails(ride);
  }
}
