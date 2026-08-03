using FluentValidation;
using Ride.Api.Shared;

namespace Ride.Api.Features.RequestRide;

public class RequestRideValidator : AbstractValidator<RequestRide>
{
  public RequestRideValidator()
  {
    RuleFor(x => x.PickupLocation).ValidCoordinate();
    RuleFor(x => x.DropoffLocation).ValidCoordinate();
  }
}
