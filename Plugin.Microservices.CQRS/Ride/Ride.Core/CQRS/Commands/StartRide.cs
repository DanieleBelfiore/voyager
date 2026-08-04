using System;
using Hikyaku;
using NetTopologySuite.Geometries;

namespace Ride.Core.CQRS.Commands
{
  public class StartRide : IRequest
  {
    public Guid Id { get; set; }
    public Guid CallerId { get; set; }
    public Point Location { get; set; }
  }
}
