using System;
using MediatR;
using NetTopologySuite.Geometries;

namespace Ride.Core.CQRS.Commands
{
  /// <summary>
  /// Records the driver's current position on the ride they are running.
  ///
  /// The ride's own LastLocation used to be written only at Start and Complete, so
  /// GET /rides/{id}/location — which reads exactly that field — reported the pickup point for
  /// the whole trip. Location updates reach Hub over SignalR and were applied to the Driver
  /// aggregate only; Ride owns its own row, so Hub has to tell it rather than reach into
  /// Driver's data.
  /// </summary>
  public class TrackRideLocation : IRequest
  {
    public Guid RideId { get; set; }
    public Point Location { get; set; }
  }
}
