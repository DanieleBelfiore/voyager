using System;
using Hikyaku;
using Ride.Module.Shared;

namespace Ride.Module.Features.GetRideDetails;

internal class GetRideDetails : IRequest<RideDetailsResponse>
{
  public Guid Id { get; set; }
  public Guid CallerId { get; set; }
}
