using System;
using MediatR;

namespace Voyager.Contracts.Ride;

/// <summary>
/// Ride lifecycle events, published by Ride and consumed by Hub to relay over SignalR to its
/// own connected clients.
///
/// In the Plugin.Microservices.CQRS variant, Ride's handlers inject
/// IHubContext&lt;VoyagerHub, IVoyagerShareClient&gt; directly and call it in-process — but
/// Ride and Hub are separate services with no SignalR backplane configured, so those calls
/// only ever reach clients connected to Ride's own (client-less) SignalR endpoint. They're
/// dead code. This variant fixes that by using the same Arbitrer notification pub/sub the
/// rest of the portfolio already relies on for cross-service requests: Ride publishes these
/// as MediatR notifications, Arbitrer fans them out over RabbitMQ, and Hub — the service that
/// actually owns connected SignalR clients — is the one that relays them.
/// </summary>
public class NewRideRequested : INotification
{
  public Guid RideId { get; set; }
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
