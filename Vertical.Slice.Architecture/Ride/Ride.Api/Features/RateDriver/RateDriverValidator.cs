using FluentValidation;

namespace Ride.Api.Features.RateDriver;

public class RateDriverValidator : AbstractValidator<RateDriver>
{
  public RateDriverValidator()
  {
    RuleFor(x => x.Rating).InclusiveBetween(Entities.Ride.MinRating, Entities.Ride.MaxRating);
  }
}
