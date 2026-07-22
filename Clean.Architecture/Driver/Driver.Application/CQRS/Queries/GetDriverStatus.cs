using System;
using Driver.Application.Dtos;
using MediatR;

namespace Driver.Application.CQRS.Queries;

public class GetDriverStatus : IRequest<DriverStatusResponse>
{
  public Guid Id { get; set; }
}
