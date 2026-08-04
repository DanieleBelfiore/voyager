using System;
using Hikyaku;
using NetTopologySuite.Geometries;

namespace Hub.Core.Ports.Primary;

/// <summary>Secondary by VoyagerHub's SignalR method directly (a connected driver client), not HTTP.</summary>
public class UpdateDriverLocation : IRequest
{
  public Guid DriverId { get; set; }
  public Point Location { get; set; }
}

public interface IUpdateDriverLocationUseCase : IRequestHandler<UpdateDriverLocation>;
