using System;
using MediatR;

namespace Driver.Core.CQRS.Commands
{
  public class AddDriver : IRequest
  {
    public Guid DriverId { get; set; }
  }
}
