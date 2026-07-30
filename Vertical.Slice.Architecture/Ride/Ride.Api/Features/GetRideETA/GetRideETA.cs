using System;
using MediatR;

namespace Ride.Api.Features.GetRideETA;

public class GetRideETA : IRequest<ETAResponse>
{
  public Guid Id { get; set; }
  public Guid CallerId { get; set; }
}
