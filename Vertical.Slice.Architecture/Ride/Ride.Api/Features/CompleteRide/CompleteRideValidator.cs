using FluentValidation;
using Ride.Api.Shared;

namespace Ride.Api.Features.CompleteRide;

// The dropoff point is what the fare is computed from, so a bogus coordinate here does not just
// crash — it changes what the rider is charged.
public class CompleteRideValidator : AbstractValidator<CompleteRide>
{
  public CompleteRideValidator()
  {
    RuleFor(x => x.Location).ValidCoordinate();
  }
}
