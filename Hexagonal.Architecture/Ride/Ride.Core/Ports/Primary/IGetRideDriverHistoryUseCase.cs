using System;
using System.Collections.Generic;
using Hikyaku;
using Ride.Core.Dtos;

namespace Ride.Core.Ports.Primary;

public class GetRideDriverHistory : IRequest<List<RideDetailsResponse>>
{
  public Guid DriverId { get; set; }
  public int Take { get; set; } = 25;
  public int Page { get; set; }
}

public interface IGetRideDriverHistoryUseCase : IRequestHandler<GetRideDriverHistory, List<RideDetailsResponse>>;
