using DriverEntity = Driver.Core.Domain.Driver;

namespace Driver.Core.Ports.Secondary;

/// <summary>Result row for IDriverRepository.GetAvailableWithinDistanceAsync — pairs a driver
/// with the exact geodetic distance the query computed, so the use case doesn't recompute it.</summary>
public class NearbyDriver
{
  public DriverEntity Driver { get; set; }
  public double DistanceInMeters { get; set; }
}
