using System;
using Driver.Application.Dtos;
using Hikyaku;

namespace Driver.Application.CQRS.Queries;

public class GetDriverStatus : IRequest<DriverStatusResponse>
{
  public Guid Id { get; set; }
}
