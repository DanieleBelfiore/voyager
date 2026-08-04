using System;
using Hikyaku;

namespace Ride.Api.Features.GetRideCurrentLocation;

public class GetRideCurrentLocation : IRequest<RideCurrentLocationResponse>
{
  public Guid Id { get; set; }
  public Guid CallerId { get; set; }
}
