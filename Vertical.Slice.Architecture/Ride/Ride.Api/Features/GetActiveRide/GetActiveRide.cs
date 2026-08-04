using System;
using Hikyaku;

namespace Ride.Api.Features.GetActiveRide;

public class GetActiveRide : IRequest<ActiveRideResponse>
{
  public Guid? DriverId { get; set; }
  public Guid? UserId { get; set; }
}
