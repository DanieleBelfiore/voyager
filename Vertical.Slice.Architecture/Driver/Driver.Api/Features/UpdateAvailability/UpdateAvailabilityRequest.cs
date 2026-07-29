using Driver.Api.Entities;

namespace Driver.Api.Features.UpdateAvailability;

public class UpdateAvailabilityRequest
{
  public DriverStatus Status { get; set; }
}
