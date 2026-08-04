using System;
using Hikyaku;
using Ride.Application.Dtos;

namespace Ride.Application.CQRS.Queries;

public class GetActiveRide : IRequest<ActiveRideResponse>
{
  public Guid? DriverId { get; set; }
  public Guid? UserId { get; set; }
}
