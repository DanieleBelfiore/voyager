using System;
using MediatR;
using Ride.Core.Dtos;

namespace Ride.Core.CQRS.Queries
{
  public class GetRideETA : IRequest<ETAResponse>
  {
    public Guid Id { get; set; }
  }
}
