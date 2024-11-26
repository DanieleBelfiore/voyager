using System;

namespace Ride.Core.Dtos
{
  public class CancelRideRequest
  {
    public Guid Id { get; set; }
    public string CancellationReason { get; set; }
  }
}
