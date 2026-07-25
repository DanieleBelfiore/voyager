using System;
using MediatR;

namespace Driver.Module.Features.GetDriverStatus;

internal class GetDriverStatus : IRequest<DriverStatusResponse>
{
  public Guid Id { get; set; }
}
