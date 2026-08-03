using FluentValidation;
using Ride.Api.Shared;

namespace Ride.Api.Features.StartRide;

public class StartRideValidator : AbstractValidator<StartRide>
{
  public StartRideValidator()
  {
    RuleFor(x => x.Location).ValidCoordinate();
  }
}
