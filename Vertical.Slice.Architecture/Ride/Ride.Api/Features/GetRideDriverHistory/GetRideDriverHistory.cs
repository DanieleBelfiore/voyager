using System;
using System.Collections.Generic;
using MediatR;
using Ride.Api.Shared;

namespace Ride.Api.Features.GetRideDriverHistory;

/// <summary>No controller wires this up — same gap that exists in the Clean/Hexagonal variants (inherited from the original Plugin implementation), not something introduced here.</summary>
public class GetRideDriverHistory : IRequest<List<RideDetailsResponse>>
{
  public Guid DriverId { get; set; }
  public int Take { get; set; } = 25;
  public int Page { get; set; }
}
