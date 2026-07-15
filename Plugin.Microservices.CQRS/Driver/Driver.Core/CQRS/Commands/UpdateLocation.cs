using System;
using MediatR;
using NetTopologySuite.Geometries;

namespace Driver.Core.CQRS.Commands
{
  public class UpdateLocation : IRequest
  {
    public Guid Id { get; set; }
    public Point Location { get; set; }
  }
}
