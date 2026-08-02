using Ride.Core.Dtos;
using Riok.Mapperly.Abstractions;
using RideEntity = Ride.Core.Domain.Ride;

namespace Ride.Core.Mapping;

[Mapper]
public partial class RideMapper
{
  [MapperIgnoreSource(nameof(RideEntity.RowVersion))]
  [MapperIgnoreSource(nameof(RideEntity.DriverRating))]
  [MapperIgnoreSource(nameof(RideEntity.RiderRating))]
  public partial RideDetailsResponse ToRideDetails(RideEntity ride);

  [MapperIgnoreSource(nameof(RideEntity.RowVersion))]
  [MapperIgnoreSource(nameof(RideEntity.DriverRating))]
  [MapperIgnoreSource(nameof(RideEntity.RiderRating))]
  public partial ActiveRideResponse ToActiveRide(RideEntity ride);
}
