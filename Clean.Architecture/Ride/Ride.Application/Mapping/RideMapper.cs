using Ride.Application.Dtos;
using Riok.Mapperly.Abstractions;
using RideEntity = Ride.Domain.Entities.Ride;

namespace Ride.Application.Mapping;

[Mapper]
public partial class RideMapper
{
  [MapperIgnoreSource(nameof(RideEntity.RowVersion))]
  public partial RideDetailsResponse ToRideDetails(RideEntity ride);

  [MapperIgnoreSource(nameof(RideEntity.RowVersion))]
  public partial ActiveRideResponse ToActiveRide(RideEntity ride);
}
