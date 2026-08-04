using System;
using Hikyaku;
using Ride.Application.Dtos;

namespace Ride.Application.CQRS.Queries;

public class GetRideCurrentLocation : IRequest<RideCurrentLocationResponse>
{
  public Guid Id { get; set; }
  public Guid CallerId { get; set; }
}
