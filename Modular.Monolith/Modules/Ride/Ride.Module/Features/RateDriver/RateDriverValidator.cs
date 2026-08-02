using FluentValidation;

namespace Ride.Module.Features.RateDriver;

/// <summary>
/// Edge validation for a nicer 400 than the domain's bare invariant. The real guarantee is
/// Ride.RateDriver itself — this is the friendly message, not the enforcement.
/// </summary>
internal class RateDriverValidator : AbstractValidator<RateDriver>
{
  public RateDriverValidator()
  {
    RuleFor(x => x.Rating).InclusiveBetween(Entities.Ride.MinRating, Entities.Ride.MaxRating);
  }
}
