using System;
using Driver.Core.Enums;
using MediatR;

namespace Driver.Core.CQRS.Commands
{
  public class UpdateAvailability : IRequest
  {
    public Guid Id { get; set; }
    public DriverStatus Status { get; set; }
  }
}
