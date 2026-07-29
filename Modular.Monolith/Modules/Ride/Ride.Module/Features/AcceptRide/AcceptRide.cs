using System;
using MediatR;

namespace Ride.Module.Features.AcceptRide;

internal class AcceptRide : IRequest
{
  public Guid DriverId { get; set; }
  public Guid RideId { get; set; }
}
