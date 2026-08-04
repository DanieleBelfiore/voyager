using System;
using Hikyaku;

namespace Ride.Application.CQRS.Commands;

public class AcceptRide : IRequest
{
  public Guid DriverId { get; set; }
  public Guid RideId { get; set; }
}
