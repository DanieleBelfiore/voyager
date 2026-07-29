using System;
using System.Threading;
using System.Threading.Tasks;
using Ride.Application.Dtos;
using Ride.Application.Ports;
using MediatR;

namespace Ride.Application.CQRS.Queries;

public class GetRideCurrentLocationHandler(IRideRepository repository) : IRequestHandler<GetRideCurrentLocation, RideCurrentLocationResponse>
{
  public async Task<RideCurrentLocationResponse> Handle(GetRideCurrentLocation request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetByIdReadOnlyAsync(request.Id, cancellationToken) ?? throw new Exception("ride_not_found");

    return new RideCurrentLocationResponse { LastLocation = ride.LastLocation, LastUpdateDate = ride.LastUpdateDate };
  }
}
