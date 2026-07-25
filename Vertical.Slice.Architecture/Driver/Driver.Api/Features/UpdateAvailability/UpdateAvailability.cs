using System;
using Driver.Api.Entities;
using MediatR;

namespace Driver.Api.Features.UpdateAvailability;

public class UpdateAvailability : IRequest
{
  public Guid Id { get; set; }
  public DriverStatus Status { get; set; }
}
