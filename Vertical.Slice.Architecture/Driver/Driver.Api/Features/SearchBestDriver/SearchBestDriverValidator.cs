using Driver.Api.Shared;
using FluentValidation;

namespace Driver.Api.Features.SearchBestDriver;

public class SearchBestDriverValidator : AbstractValidator<SearchBestDriver>
{
  public SearchBestDriverValidator()
  {
    RuleFor(x => x.Location).ValidCoordinate();
    // Also the divisor for normalizedDistance in the scoring formula: at zero every candidate
    // scores NaN and the ranking collapses. The controller clamps it, but this request is also
    // reachable straight through the mediator.
    RuleFor(x => x.DistanceThresholdInMeters).GreaterThan(0);
  }
}
