using System;
using Hikyaku;

namespace Driver.Api.Features.GetDriverStatus;

public class GetDriverStatus : IRequest<DriverStatusResponse>
{
  public Guid Id { get; set; }
}
