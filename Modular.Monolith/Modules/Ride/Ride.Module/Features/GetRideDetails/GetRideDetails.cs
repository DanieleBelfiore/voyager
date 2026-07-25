using System;
using MediatR;
using Ride.Module.Shared;

namespace Ride.Module.Features.GetRideDetails;

internal class GetRideDetails : IRequest<RideDetailsResponse>
{
  public Guid Id { get; set; }
}
