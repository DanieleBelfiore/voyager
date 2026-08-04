using System;
using Hikyaku;

namespace Ride.Api.Features.GetRideETA;

public class GetRideETA : IRequest<ETAResponse>
{
  public Guid Id { get; set; }
  public Guid CallerId { get; set; }
}
