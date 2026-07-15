using System;
using MediatR;

namespace Identity.Core.CQRS.Commands
{
  public class UpdateUserRating : IRequest<double>
  {
    public Guid UserId { get; set; }
    public int Rating { get; set; }
    public int Rides { get; set; }
  }
}
