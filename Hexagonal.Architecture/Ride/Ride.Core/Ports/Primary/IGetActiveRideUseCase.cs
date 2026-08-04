using System;
using Hikyaku;
using Ride.Core.Dtos;

namespace Ride.Core.Ports.Primary;

public class GetActiveRide : IRequest<ActiveRideResponse>
{
  public Guid? DriverId { get; set; }
  public Guid? UserId { get; set; }
}

public interface IGetActiveRideUseCase : IRequestHandler<GetActiveRide, ActiveRideResponse>;
