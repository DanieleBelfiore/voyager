using System;
using Hikyaku;
using NetTopologySuite.Geometries;
using Ride.Core.Dtos;

namespace Ride.Core.Ports.Primary;

public class RequestRide : IRequest<RideDetailsResponse>
{
  public Guid UserId { get; set; }
  public Guid DriverId { get; set; }
  public Point PickupLocation { get; set; }
  public Point DropoffLocation { get; set; }
}

public interface IRequestRideUseCase : IRequestHandler<RequestRide, RideDetailsResponse>;
