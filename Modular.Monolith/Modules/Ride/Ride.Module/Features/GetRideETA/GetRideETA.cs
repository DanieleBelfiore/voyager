using System;
using MediatR;

namespace Ride.Module.Features.GetRideETA;

internal class GetRideETA : IRequest<ETAResponse>
{
  public Guid Id { get; set; }
}
