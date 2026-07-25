using System;
using MediatR;

namespace Ride.Api.Features.GetRideCurrentLocation;

public class GetRideCurrentLocation : IRequest<RideCurrentLocationResponse>
{
  public Guid Id { get; set; }
}
