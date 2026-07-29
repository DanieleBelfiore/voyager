using System;
using Driver.Domain.Enums;
using MediatR;

namespace Driver.Application.CQRS.Commands;

public class UpdateAvailability : IRequest
{
  public Guid Id { get; set; }
  public DriverStatus Status { get; set; }
}
