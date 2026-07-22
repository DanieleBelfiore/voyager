using System;
using MediatR;
using Ride.Application.Dtos;

namespace Ride.Application.CQRS.Queries;

public class GetRideDetails : IRequest<RideDetailsResponse>
{
  public Guid Id { get; set; }
}
