using System;
using MediatR;
using Ride.Core.Dtos;

namespace Ride.Core.Ports.Primary;

public class GetRideDetails : IRequest<RideDetailsResponse>
{
  public Guid Id { get; set; }
}

public interface IGetRideDetailsUseCase : IRequestHandler<GetRideDetails, RideDetailsResponse>;
