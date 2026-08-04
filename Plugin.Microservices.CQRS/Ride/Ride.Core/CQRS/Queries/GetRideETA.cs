using System;
using Hikyaku;
using Ride.Core.Dtos;

namespace Ride.Core.CQRS.Queries
{
  public class GetRideETA : IRequest<ETAResponse>
  {
    public Guid Id { get; set; }
    public Guid CallerId { get; set; }
  }
}
