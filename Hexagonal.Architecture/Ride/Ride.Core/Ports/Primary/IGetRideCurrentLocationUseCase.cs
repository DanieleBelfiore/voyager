using System;
using Hikyaku;
using Ride.Core.Dtos;

namespace Ride.Core.Ports.Primary;

public class GetRideCurrentLocation : IRequest<RideCurrentLocationResponse>
{
  public Guid Id { get; set; }
  public Guid CallerId { get; set; }
}

public interface IGetRideCurrentLocationUseCase : IRequestHandler<GetRideCurrentLocation, RideCurrentLocationResponse>;
