using Ride.Core.Dtos;
using Riok.Mapperly.Abstractions;
using RideEntity = Ride.Core.Domain.Ride;

namespace Ride.Core.Mapping;

[Mapper]
public partial class RideMapper
{
  public partial RideDetailsResponse ToRideDetails(RideEntity ride);
  public partial ActiveRideResponse ToActiveRide(RideEntity ride);
}
