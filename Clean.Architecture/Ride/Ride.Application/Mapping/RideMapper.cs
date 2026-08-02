using Ride.Application.Dtos;
using Riok.Mapperly.Abstractions;
using RideEntity = Ride.Domain.Entities.Ride;

namespace Ride.Application.Mapping;

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
