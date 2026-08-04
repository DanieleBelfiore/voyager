using System;
using Hikyaku;
using NetTopologySuite.Geometries;

namespace Ride.Module.Features.StartRide;

internal class StartRide : IRequest
{
  public Guid Id { get; set; }
  public Point Location { get; set; }
  public Guid CallerId { get; set; }
}
