using System;
using MediatR;
using NetTopologySuite.Geometries;

namespace Hub.Module.Features.UpdateDriverLocation;

internal class UpdateDriverLocation : IRequest
{
  public Guid DriverId { get; set; }
  public Point Location { get; set; }
}
