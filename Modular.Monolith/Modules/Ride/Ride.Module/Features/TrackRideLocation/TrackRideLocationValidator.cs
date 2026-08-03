using FluentValidation;
using Ride.Module.Shared;

namespace Ride.Module.Features.TrackRideLocation;

internal class TrackRideLocationValidator : AbstractValidator<Voyager.Contracts.Ride.TrackRideLocation>
{
  public TrackRideLocationValidator()
  {
    RuleFor(x => x.Location).ValidCoordinate();
  }
}
