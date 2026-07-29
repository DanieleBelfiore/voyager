using System;
using System.Threading;
using System.Threading.Tasks;
using Ride.Application.Dtos;
using Ride.Application.Mapping;
using Ride.Application.Ports;
using MediatR;

namespace Ride.Application.CQRS.Queries;

public class GetRideDetailsHandler(IRideRepository repository, RideMapper mapper) : IRequestHandler<GetRideDetails, RideDetailsResponse>
{
  public async Task<RideDetailsResponse> Handle(GetRideDetails request, CancellationToken cancellationToken)
  {
    var ride = await repository.GetByIdReadOnlyAsync(request.Id, cancellationToken) ?? throw new Exception("ride_not_found");

    return mapper.ToRideDetails(ride);
  }
}
