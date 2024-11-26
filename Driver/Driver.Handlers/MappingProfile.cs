using AutoMapper;
using Driver.Core.Dtos;

namespace Driver.Handlers
{
  public class MappingProfile : Profile
  {
    public MappingProfile()
    {
      CreateMap<Models.Driver, DriverStatusResponse>();
    }
  }
}
