using Driver.Module.Entities;

namespace Driver.Module.Features.UpdateAvailability;

/// <summary>Public — bound from the request body on a public controller action, so it can't be internal (CS0050).</summary>
public class UpdateAvailabilityRequest
{
  public DriverStatus Status { get; set; }
}
