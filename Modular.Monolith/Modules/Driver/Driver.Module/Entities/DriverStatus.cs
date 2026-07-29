namespace Driver.Module.Entities;

/// <summary>
/// Public, unlike the rest of Entities/ — it's part of the wire shape returned by
/// GetDriverStatus and accepted by UpdateAvailability, so it has to cross the same
/// public/internal line those DTOs do. A public method can never expose a less-accessible
/// type (CS0050), which is what actually drives the internal/public split in this module,
/// not the Entities/Features folder boundary.
/// </summary>
public enum DriverStatus
{
  Offline,
  Available,
  OnRide
}
