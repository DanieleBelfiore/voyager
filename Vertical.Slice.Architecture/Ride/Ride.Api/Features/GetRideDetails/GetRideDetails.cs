using System;
using Hikyaku;
using Ride.Api.Shared;

namespace Ride.Api.Features.GetRideDetails;

public class GetRideDetails : IRequest<RideDetailsResponse>
{
  public Guid Id { get; set; }
  public Guid CallerId { get; set; }
}
