using System;
using Driver.Core.Domain;
using MediatR;

namespace Driver.Core.Ports.Primary;

public class UpdateAvailability : IRequest
{
  public Guid Id { get; set; }
  public DriverStatus Status { get; set; }
}

public interface IUpdateAvailabilityUseCase : IRequestHandler<UpdateAvailability>;
