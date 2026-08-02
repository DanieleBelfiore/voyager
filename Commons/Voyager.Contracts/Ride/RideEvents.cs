using System;
using MediatR;

namespace Voyager.Contracts.Ride;

/// <summary>
/// Ride lifecycle events, published by Ride and consumed by Hub to relay over SignalR to its
/// own connected clients. Every variant — including Plugin.Microservices.CQRS, whose original
/// handlers used to call IHubContext&lt;VoyagerHub, IVoyagerShareClient&gt; directly in-process
/// and never reached a real client, since Ride and Hub are separate services — publishes these
/// as MediatR notifications; Arbitrer fans them out over RabbitMQ to whichever service owns the
/// connected SignalR clients (Hub), where they're relayed for real.
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
