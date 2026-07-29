using Driver.Application.Dtos;
using Riok.Mapperly.Abstractions;
using DriverEntity = Driver.Domain.Entities.Driver;

namespace Driver.Application.Mapping;

[Mapper]
public partial class DriverMapper
{
  public partial DriverStatusResponse ToDto(DriverEntity driver);
}
