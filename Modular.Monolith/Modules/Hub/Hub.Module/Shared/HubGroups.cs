using System;

namespace Hub.Module.Shared;

internal static class HubGroups
{
  public static string ForRide(Guid rideId) => $"ride_{rideId}";
}
