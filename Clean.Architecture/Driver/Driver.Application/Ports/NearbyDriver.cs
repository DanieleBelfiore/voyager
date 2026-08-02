namespace Driver.Application.Ports;

/// <summary>Result row for IDriverRepository.GetAvailableWithinDistanceAsync — pairs a driver
/// with the exact geodetic distance the query computed, so the handler doesn't recompute it.</summary>
public class NearbyDriver
{
  public Domain.Entities.Driver Driver { get; set; }
  public double DistanceInMeters { get; set; }
}
