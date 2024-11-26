using System;
using Driver.Core.Dtos;
using MediatR;

namespace Driver.Core.CQRS.Queries
{
  public class GetDriverStatus : IRequest<DriverStatusResponse>
  {
    public Guid Id { get; set; }
  }
}
