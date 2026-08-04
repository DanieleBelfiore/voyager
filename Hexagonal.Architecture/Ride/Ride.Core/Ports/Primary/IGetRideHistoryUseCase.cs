using System;
using System.Collections.Generic;
using Hikyaku;
using Ride.Core.Dtos;

namespace Ride.Core.Ports.Primary;

public class GetRideHistory : IRequest<List<RideDetailsResponse>>
{
  public Guid UserId { get; set; }
  public int Take { get; set; } = 25;
  public int Page { get; set; }
}

public interface IGetRideHistoryUseCase : IRequestHandler<GetRideHistory, List<RideDetailsResponse>>;
