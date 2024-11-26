using System;
using MediatR;
using NetTopologySuite.Geometries;

namespace Ride.Core.CQRS.Commands
{
  public class StartRide : IRequest
  {
    public Guid Id { get; set; }
    public Point Location { get; set; }
  }
}
