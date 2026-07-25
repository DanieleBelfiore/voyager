using System;

namespace Hub.Api.Shared;

/// <summary>Shared by UpdateDriverLocation and RideEvents — both push to the same per-ride SignalR group.</summary>
public static class HubGroups
{
  public static string ForRide(Guid rideId) => $"ride_{rideId}";
}
