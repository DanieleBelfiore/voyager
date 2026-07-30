using System;
using MediatR;

namespace Ride.Core.Ports.Primary;

public class RateDriver : IRequest
{
  public Guid RideId { get; set; }
  public int Rating { get; set; }
  public Guid CallerId { get; set; }
}

public interface IRateDriverUseCase : IRequestHandler<RateDriver>;
