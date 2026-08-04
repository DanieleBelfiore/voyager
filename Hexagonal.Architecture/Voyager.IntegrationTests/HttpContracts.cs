using NetTopologySuite.Geometries;

namespace Voyager.IntegrationTests;

/// <summary>
/// The request and response shapes as they appear on the wire.
/// </summary>
/// <remarks>
/// Declared here rather than referenced out of the service assemblies. Each host is behind an
/// extern alias (four top-level <c>Program</c> types in one test assembly), so using their DTOs
/// directly means alias-qualifying every type; more to the point, an integration test that binds
/// to the server's own classes stops being able to notice a shape change that would break a real
/// client. These mirror the contract, and drift shows up as a failing test.
/// </remarks>
public class SearchBestDriverPayload
{
  public Point Location { get; set; }
  public int DistanceThresholdInKm { get; set; }
}

public class SearchBestDriverResult
{
  public Guid DriverId { get; set; }
  public Point LastLocation { get; set; }
  public double Distance { get; set; }
  public double Score { get; set; }
}

public class RequestRidePayload
{
  public Guid DriverId { get; set; }
  public Point PickupLocation { get; set; }
  public Point DropoffLocation { get; set; }
}

public class RideDetailsResult
{
  public Guid Id { get; set; }
  public Guid UserId { get; set; }
  public Guid DriverId { get; set; }
  public DateTime? StartAt { get; set; }
  public DateTime? EndAt { get; set; }
  public double? Price { get; set; }
  public string Status { get; set; }
  public Point PickupLocation { get; set; }
  public Point DropoffLocation { get; set; }
}

/// <summary>
/// Mirrors the services' <c>DriverStatus</c>. Sent as a string because every host registers
/// Newtonsoft's <c>StringEnumConverter</c>.
/// </summary>
public static class DriverAvailability
{
  public const string Available = "Available";
}

public static class RideState
{
  public const string Requested = "Requested";
  public const string DriverAssigned = "DriverAssigned";
  public const string InProgress = "InProgress";
  public const string Completed = "Completed";
}
