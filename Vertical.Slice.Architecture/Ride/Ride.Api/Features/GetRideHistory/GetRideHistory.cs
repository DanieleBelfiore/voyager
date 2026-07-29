using System;
using System.Collections.Generic;
using MediatR;
using Ride.Api.Shared;

namespace Ride.Api.Features.GetRideHistory;

public class GetRideHistory : IRequest<List<RideDetailsResponse>>
{
  public Guid UserId { get; set; }
  public int Take { get; set; } = 25;
  public int Page { get; set; }
}
