using Ride.Application.Dtos;
using Riok.Mapperly.Abstractions;
using RideEntity = Ride.Domain.Entities.Ride;

namespace Ride.Application.Mapping;

[Mapper]
public partial class RideMapper
{
  public partial RideDetailsResponse ToRideDetails(RideEntity ride);
  public partial ActiveRideResponse ToActiveRide(RideEntity ride);
}
