using System;
using MediatR;
using Ride.Core.Dtos;

namespace Ride.Core.CQRS.Queries
{
  public class GetRideCurrentLocation : IRequest<RideCurrentLocationResponse>
  {
    public Guid Id { get; set; }
  }
}
