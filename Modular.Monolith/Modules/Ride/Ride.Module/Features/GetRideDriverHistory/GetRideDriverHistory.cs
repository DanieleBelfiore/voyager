using System;
using System.Collections.Generic;
using MediatR;
using Ride.Module.Shared;

namespace Ride.Module.Features.GetRideDriverHistory;

/// <summary>No controller wires this up — same gap that exists in every other variant (inherited from the original Plugin implementation), not something introduced here.</summary>
internal class GetRideDriverHistory : IRequest<List<RideDetailsResponse>>
{
  public Guid DriverId { get; set; }
  public int Take { get; set; } = 25;
  public int Page { get; set; }
}
