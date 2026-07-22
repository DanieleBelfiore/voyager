using Driver.Core.Dtos;
using Riok.Mapperly.Abstractions;
using DriverEntity = Driver.Core.Domain.Driver;

namespace Driver.Core.Mapping;

[Mapper]
public partial class DriverMapper
{
  public partial DriverStatusResponse ToDto(DriverEntity driver);
}
