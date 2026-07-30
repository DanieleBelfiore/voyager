using System;
using MediatR;
using Ride.Core.Dtos;

namespace Ride.Core.Ports.Primary;

public class GetRideETA : IRequest<ETAResponse>
{
  public Guid Id { get; set; }
  public Guid CallerId { get; set; }
}

public interface IGetRideETAUseCase : IRequestHandler<GetRideETA, ETAResponse>;
