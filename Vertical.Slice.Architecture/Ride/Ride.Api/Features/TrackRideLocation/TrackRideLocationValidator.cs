using FluentValidation;
using Ride.Api.Shared;

namespace Ride.Api.Features.TrackRideLocation;

public class TrackRideLocationValidator : AbstractValidator<Voyager.Contracts.Ride.TrackRideLocation>
{
  public TrackRideLocationValidator()
  {
    RuleFor(x => x.Location).ValidCoordinate();
  }
}
