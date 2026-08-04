using System;
using Hikyaku;

namespace Ride.Module.Features.AcceptRide;

internal class AcceptRide : IRequest
{
  public Guid DriverId { get; set; }
  public Guid RideId { get; set; }
}
