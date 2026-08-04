using System;
using Driver.Module.Entities;
using Hikyaku;

namespace Driver.Module.Features.UpdateAvailability;

internal class UpdateAvailability : IRequest
{
  public Guid Id { get; set; }
  public DriverStatus Status { get; set; }
}
