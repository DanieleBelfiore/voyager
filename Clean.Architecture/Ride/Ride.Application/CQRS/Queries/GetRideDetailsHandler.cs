using System;
using System.Collections.Generic;
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
    var ride = await repository.GetByIdReadOnlyAsync(request.Id, cancellationToken) ?? throw new KeyNotFoundException("ride_not_found");

    if (ride.UserId != request.CallerId && ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    return mapper.ToRideDetails(ride);
  }
}
