using AutoMapper;
using Ride.Core.Dtos;

namespace Ride.Handlers
{
  public class MappingProfile : Profile
  {
    public MappingProfile()
    {
      CreateMap<Models.Ride, ActiveRideResponse>();
      CreateMap<Models.Ride, RideDetailsResponse>();
    }
  }
}
