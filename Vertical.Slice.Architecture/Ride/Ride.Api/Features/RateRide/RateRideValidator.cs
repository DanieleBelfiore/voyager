using FluentValidation;

namespace Ride.Api.Features.RateRide;

public class RateRideValidator : AbstractValidator<RateRide>
{
  public RateRideValidator()
  {
    RuleFor(x => x.Rating).InclusiveBetween(Entities.Ride.MinRating, Entities.Ride.MaxRating);
  }
}
