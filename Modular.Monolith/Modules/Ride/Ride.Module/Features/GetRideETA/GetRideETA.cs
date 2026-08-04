using System;
using Hikyaku;

namespace Ride.Module.Features.GetRideETA;

internal class GetRideETA : IRequest<ETAResponse>
{
  public Guid Id { get; set; }
  public Guid CallerId { get; set; }
}
