using System;
using Hikyaku;
using NetTopologySuite.Geometries;

namespace Ride.Application.CQRS.Commands;

public class StartRide : IRequest
{
  public Guid Id { get; set; }
  public Point Location { get; set; }
  public Guid CallerId { get; set; }
}
