using System;
using Hikyaku;

namespace Driver.Module.Features.GetDriverStatus;

internal class GetDriverStatus : IRequest<DriverStatusResponse>
{
  public Guid Id { get; set; }

  /// <summary>Set from the token by the controller, never from the request — the handler
  /// compares it against <see cref="Id"/> to keep a driver's live position self-read only.</summary>
  public Guid CallerId { get; set; }
}
