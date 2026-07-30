using FluentValidation;

namespace Ride.Api.Features.RateDriver;

public class RateDriverValidator : AbstractValidator<RateDriver>
{
  public RateDriverValidator()
  {
    RuleFor(x => x.Rating).InclusiveBetween(1, 5);
  }
}
