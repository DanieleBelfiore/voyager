using System;
using MediatR;
using NetTopologySuite.Geometries;

namespace Ride.Api.Features.StartRide;

public class StartRide : IRequest
{
  public Guid Id { get; set; }
  public Point Location { get; set; }
}
