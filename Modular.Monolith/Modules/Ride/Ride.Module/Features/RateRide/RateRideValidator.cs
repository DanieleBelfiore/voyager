using FluentValidation;

namespace Ride.Module.Features.RateRide;

/// <summary>
/// Edge validation for a nicer 400 than the domain's bare invariant. The real guarantee is
/// Ride.RateRide itself — this is the friendly message, not the enforcement.
/// </summary>
internal class RateRideValidator : AbstractValidator<RateRide>
{
  public RateRideValidator()
  {
    RuleFor(x => x.Rating).InclusiveBetween(Entities.Ride.MinRating, Entities.Ride.MaxRating);
  }
}
