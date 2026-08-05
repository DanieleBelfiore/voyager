using System;
using Hikyaku;

namespace Driver.Core.CQRS.Queries
{
  /// <summary>
  /// Cross-service query: is this driver known, and can they take a ride right now. Ride sends it
  /// before creating a ride. Deliberately narrower than <see cref="GetDriverStatus"/>, which is a
  /// user-facing query gated on the caller owning the driver record — reusing that one here would
  /// force a service-to-service call to carry an end-user identity it does not have.
  /// </summary>
  public class GetDriverAvailability : IRequest<DriverAvailabilityInfo>
  {
    public Guid DriverId { get; set; }
  }

  public class DriverAvailabilityInfo
  {
    public bool Exists { get; set; }
    public bool IsAvailable { get; set; }
  }
}
