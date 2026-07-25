using System;
using MediatR;
using NetTopologySuite.Geometries;

namespace Hub.Api.Features.UpdateDriverLocation;

/// <summary>Reached from VoyagerHub's SignalR method (a connected driver client), not HTTP.</summary>
public class UpdateDriverLocation : IRequest
{
  public Guid DriverId { get; set; }
  public Point Location { get; set; }
}
