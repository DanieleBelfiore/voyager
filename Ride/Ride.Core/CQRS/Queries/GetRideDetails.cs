using System;
using MediatR;
using Ride.Core.Dtos;

namespace Ride.Core.CQRS.Queries
{
  public class GetRideDetails : IRequest<RideDetailsResponse>
  {
    public Guid Id { get; set; }
  }
}
