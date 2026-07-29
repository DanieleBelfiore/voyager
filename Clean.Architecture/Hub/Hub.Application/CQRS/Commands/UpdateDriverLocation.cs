using System;
using MediatR;
using NetTopologySuite.Geometries;

namespace Hub.Application.CQRS.Commands;

/// <summary>
/// Driven by a connected driver client calling the SignalR hub method directly — not by an
/// HTTP request, hence living in Application rather than being routed through a REST controller.
/// </summary>
public class UpdateDriverLocation : IRequest
{
  public Guid DriverId { get; set; }
  public Point Location { get; set; }
}
