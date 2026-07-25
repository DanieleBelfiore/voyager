using System;
using MediatR;

namespace Driver.Api.Features.GetDriverStatus;

public class GetDriverStatus : IRequest<DriverStatusResponse>
{
  public Guid Id { get; set; }
}
