using FluentValidation;
using Ride.Module.Shared;

namespace Ride.Module.Features.CompleteRide;

// The dropoff point is what the fare is computed from, so a bogus coordinate here does not just
// crash — it changes what the rider is charged.
internal class CompleteRideValidator : AbstractValidator<CompleteRide>
{
  public CompleteRideValidator()
  {
    RuleFor(x => x.Location).ValidCoordinate();
  }
}
