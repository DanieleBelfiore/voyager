using System;
using MediatR;

namespace Ride.Core.Ports.Primary;

public class RateRide : IRequest
{
  public Guid RideId { get; set; }
  public int Rating { get; set; }
}

public interface IRateRideUseCase : IRequestHandler<RateRide>;
