using System;
using MediatR;

namespace Ride.Core.Ports.Primary;

public class AcceptRide : IRequest
{
  public Guid DriverId { get; set; }
  public Guid RideId { get; set; }
}

public interface IAcceptRideUseCase : IRequestHandler<AcceptRide>;
