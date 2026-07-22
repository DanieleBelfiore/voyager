using System;
using System.Collections.Generic;
using MediatR;
using Ride.Application.Dtos;

namespace Ride.Application.CQRS.Queries;

public class GetRideDriverHistory : IRequest<List<RideDetailsResponse>>
{
  public Guid DriverId { get; set; }
  public int Take { get; set; } = 25;
  public int Page { get; set; }
}
