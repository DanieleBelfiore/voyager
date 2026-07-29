using System.Linq;
using Ride.Core.Dtos;
using Riok.Mapperly.Abstractions;
using RideModel = Ride.Handlers.Models.Ride;

namespace Ride.Handlers;

[Mapper]
public partial class RideMapper
{
  [MapperIgnoreSource(nameof(RideModel.PickupLocationGeoJSON))]
  [MapperIgnoreSource(nameof(RideModel.DropoffLocationGeoJSON))]
  [MapperIgnoreSource(nameof(RideModel.LastLocationGeoJSON))]
  public partial RideDetailsResponse ToRideDetails(RideModel ride);

  [MapperIgnoreSource(nameof(RideModel.PickupLocationGeoJSON))]
  [MapperIgnoreSource(nameof(RideModel.DropoffLocationGeoJSON))]
  [MapperIgnoreSource(nameof(RideModel.LastLocationGeoJSON))]
  public partial ActiveRideResponse ToActiveRide(RideModel ride);
  public partial IQueryable<RideDetailsResponse> ProjectToRideDetails(IQueryable<RideModel> source);
  public partial IQueryable<ActiveRideResponse> ProjectToActiveRide(IQueryable<RideModel> source);
}
