using System;
using Hikyaku;

namespace Ride.Module.Features.GetActiveRide;

internal class GetActiveRide : IRequest<ActiveRideResponse>
{
  public Guid? DriverId { get; set; }
  public Guid? UserId { get; set; }
}
