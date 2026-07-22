using Driver.Core.Domain;

namespace Driver.Core.Dtos;

public class UpdateAvailabilityRequest
{
  public DriverStatus Status { get; set; }
}
