using System;

namespace Hub.Module.Shared;

internal static class HubGroups
{
  public static string ForRide(Guid rideId) => $"ride_{rideId}";

  /// <summary>Connection-scoped personal group, joined by every client on connect (see
  /// VoyagerHub.OnConnectedAsync). Used to reach a specific user before any ride group exists
  /// to join — e.g. NewRideRequested, which fires while the ride is still Requested.</summary>
  public static string ForUser(Guid userId) => $"user_{userId}";
}
