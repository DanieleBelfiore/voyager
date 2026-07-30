using FluentValidation;

namespace Ride.Api.Features.RateRide;

public class RateRideValidator : AbstractValidator<RateRide>
{
  public RateRideValidator()
  {
    RuleFor(x => x.Rating).InclusiveBetween(1, 5);
  }
}
