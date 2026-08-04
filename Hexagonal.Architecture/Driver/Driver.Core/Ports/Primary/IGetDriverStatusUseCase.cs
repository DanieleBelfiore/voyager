using System;
using Driver.Core.Dtos;
using Hikyaku;

namespace Driver.Core.Ports.Primary;

public class GetDriverStatus : IRequest<DriverStatusResponse>
{
  public Guid Id { get; set; }
}

public interface IGetDriverStatusUseCase : IRequestHandler<GetDriverStatus, DriverStatusResponse>;
