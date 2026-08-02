using System;
using MediatR;

namespace Ride.Core.CQRS.Events
{
  /// <summary>
  /// Ride lifecycle events, published by Ride.Handlers and consumed by Hub.API to relay over
  /// SignalR to its own connected clients. Ride and Hub are separate processes, so Arbitrer's
  /// remote notification fan-out over RabbitMQ is what actually delivers these — the direct
  /// IHubContext&lt;VoyagerHub, IVoyagerShareClient&gt; injection this replaced only ever reached
  /// clients connected to Ride's own (client-less) SignalR endpoint.
  /// </summary>
  public class NewRideRequested : INotification
  {
    public Guid RideId { get; set; }

    /// <summary>Routes the notification to the driver's personal group (user_{DriverId}) — at
    /// Requested status nobody has joined ride_{RideId} yet, so that group would be empty.</summary>
    public Guid DriverId { get; set; }
  }

  public class RideAccepted : INotification
  {
    public Guid RideId { get; set; }
  }

  public class RideCancelled : INotification
  {
    public Guid RideId { get; set; }
  }

  public class RideCompleted : INotification
  {
    public Guid RideId { get; set; }
  }

  public class DriverRatingReceived : INotification
  {
    public Guid RideId { get; set; }
    public int Rating { get; set; }
  }

  public class RiderRatingReceived : INotification
  {
    public Guid RideId { get; set; }
    public int Rating { get; set; }
  }
}
