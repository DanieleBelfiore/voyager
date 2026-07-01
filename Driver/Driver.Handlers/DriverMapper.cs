using System.Linq;
using Driver.Core.Dtos;
using Riok.Mapperly.Abstractions;
using DriverModel = Driver.Handlers.Models.Driver;

namespace Driver.Handlers;

[Mapper]
public partial class DriverMapper
{
  [MapperIgnoreTarget(nameof(DriverStatusResponse.LicenseNumber))]
  [MapperIgnoreTarget(nameof(DriverStatusResponse.VehicleInfo))]
  [MapperIgnoreSource(nameof(DriverModel.LastLocationGeoJSON))]
  public partial DriverStatusResponse ToDto(DriverModel driver);

  public partial IQueryable<DriverStatusResponse> ProjectToDto(IQueryable<DriverModel> source);
}
