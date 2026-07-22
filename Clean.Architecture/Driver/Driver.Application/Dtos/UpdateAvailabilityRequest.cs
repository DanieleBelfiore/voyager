using Driver.Domain.Enums;

namespace Driver.Application.Dtos;

public class UpdateAvailabilityRequest
{
  public DriverStatus Status { get; set; }
}
