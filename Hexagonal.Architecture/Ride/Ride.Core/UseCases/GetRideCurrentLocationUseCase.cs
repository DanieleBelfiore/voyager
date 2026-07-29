using System;
using System.Threading;
using System.Threading.Tasks;
using Ride.Core.Dtos;
using Ride.Core.Ports.Secondary;
using Ride.Core.Ports.Primary;

namespace Ride.Core.UseCases;

public class GetRideCurrentLocationUseCase(IRideRepository repository) : IGetRideCurrentLocationUseCase
{
  public async Task<RideCurrentLocationResponse> Handle(GetRideCurrentLocation request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetByIdReadOnlyAsync(request.Id, cancellationToken) ?? throw new Exception("ride_not_found");

    return new RideCurrentLocationResponse { LastLocation = ride.LastLocation, LastUpdateDate = ride.LastUpdateDate };
  }
}
