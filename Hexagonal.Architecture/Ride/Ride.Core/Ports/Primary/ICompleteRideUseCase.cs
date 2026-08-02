using System;
using MediatR;
using NetTopologySuite.Geometries;

namespace Ride.Core.Ports.Primary;

public class CompleteRide : IRequest
{
  public Guid Id { get; set; }
  public Point Location { get; set; }
  public Guid CallerId { get; set; }
}

public interface ICompleteRideUseCase : IRequestHandler<CompleteRide>;
