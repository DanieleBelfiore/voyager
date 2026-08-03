using FluentValidation;
using Ride.Module.Shared;

namespace Ride.Module.Features.RequestRide;

internal class RequestRideValidator : AbstractValidator<RequestRide>
{
  public RequestRideValidator()
  {
    RuleFor(x => x.PickupLocation).ValidCoordinate();
    RuleFor(x => x.DropoffLocation).ValidCoordinate();
  }
}
