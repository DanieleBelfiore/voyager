using System;
using MediatR;
using Ride.Api.Shared;

namespace Ride.Api.Features.GetRideDetails;

public class GetRideDetails : IRequest<RideDetailsResponse>
{
  public Guid Id { get; set; }
}
