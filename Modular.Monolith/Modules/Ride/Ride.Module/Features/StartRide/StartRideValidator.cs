using FluentValidation;
using Ride.Module.Shared;

namespace Ride.Module.Features.StartRide;

internal class StartRideValidator : AbstractValidator<StartRide>
{
  public StartRideValidator()
  {
    RuleFor(x => x.Location).ValidCoordinate();
  }
}
