using System;
using MediatR;
using NetTopologySuite.Geometries;

namespace Ride.Module.Features.CompleteRide;

internal class CompleteRide : IRequest
{
  public Guid Id { get; set; }
  public Point Location { get; set; }
  public double Price { get; set; }
  public Guid CallerId { get; set; }
}
