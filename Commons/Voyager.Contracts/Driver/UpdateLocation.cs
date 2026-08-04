using System;
using Hikyaku;
using NetTopologySuite.Geometries;

namespace Voyager.Contracts.Driver;

/// <summary>
/// Cross-service command: update a driver's live location, owned by the Driver bounded
/// context. Sent remotely by Hub when a connected driver client reports a new position.
/// </summary>
public class UpdateLocation : IRequest
{
  public Guid Id { get; set; }
  public Point Location { get; set; }
}
