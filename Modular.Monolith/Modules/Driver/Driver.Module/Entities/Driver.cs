using System;
using NetTopologySuite.Geometries;

namespace Driver.Module.Entities;

/// <summary>
/// Aggregate root for the Driver bounded context. Internal — no other module can reference
/// this type directly, even though everything runs in the same process. The module boundary
/// here is enforced by C# visibility, not a network hop.
/// </summary>
internal class Driver
{
  public Guid Id { get; private set; }
  public DriverStatus Status { get; private set; } = DriverStatus.Available;
  public Point LastLocation { get; private set; }
  public DateTime LastUpdateDate { get; private set; } = DateTime.UtcNow;

  private Driver()
  {
    // EF Core
  }

  public Driver(Guid id)
  {
    Id = id;
  }

  public void UpdateAvailability(DriverStatus status)
  {
    Status = status;
    LastUpdateDate = DateTime.UtcNow;
  }

  public void UpdateLocation(Point location)
  {
    if (location == null)
      throw new ArgumentNullException(nameof(location));

    LastLocation = location;
    LastUpdateDate = DateTime.UtcNow;
  }
}
