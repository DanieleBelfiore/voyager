using System;
using Hikyaku;

namespace Ride.Module.Features.GetRideCurrentLocation;

internal class GetRideCurrentLocation : IRequest<RideCurrentLocationResponse>
{
  public Guid Id { get; set; }
  public Guid CallerId { get; set; }
}
