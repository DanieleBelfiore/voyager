using System;
using MediatR;

namespace Ride.Api.Features.AcceptRide;

public class AcceptRide : IRequest
{
  public Guid DriverId { get; set; }
  public Guid RideId { get; set; }
}
