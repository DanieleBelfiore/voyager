using System;
using System.Collections.Generic;
using Hikyaku;
using Ride.Module.Shared;

namespace Ride.Module.Features.GetRideHistory;

internal class GetRideHistory : IRequest<List<RideDetailsResponse>>
{
  public Guid UserId { get; set; }
  public int Take { get; set; } = 25;
  public int Page { get; set; }
}
