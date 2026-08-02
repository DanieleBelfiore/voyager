using System;
using MediatR;
using NetTopologySuite.Geometries;

namespace Voyager.Contracts.Ride;

/// <summary>
/// Cross-service command: record the driver's current position on the ride they are running.
///
/// The ride's own LastLocation used to be written only at Start and Complete, so
/// GET /rides/{id}/location — which reads exactly that field — reported the pickup point for the
/// whole trip. Location updates reach Hub over SignalR and were applied to the Driver aggregate
/// only; Ride owns its own row, so Hub has to tell it rather than reach into Driver's data.
///
/// Fire-and-forget by design: a dropped position is superseded by the next one a few seconds
/// later, and the SignalR push to the rider must not wait on Ride's write.
/// </summary>
public class TrackRideLocation : IRequest
{
  public Guid RideId { get; set; }
  public Point Location { get; set; }
}
