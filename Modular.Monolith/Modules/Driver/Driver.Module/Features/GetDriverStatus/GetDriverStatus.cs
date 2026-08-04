using System;
using Hikyaku;

namespace Driver.Module.Features.GetDriverStatus;

internal class GetDriverStatus : IRequest<DriverStatusResponse>
{
  public Guid Id { get; set; }
}
