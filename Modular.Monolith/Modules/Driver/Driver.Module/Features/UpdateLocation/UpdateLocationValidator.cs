using Driver.Module.Shared;
using FluentValidation;

namespace Driver.Module.Features.UpdateLocation;

internal class UpdateLocationValidator : AbstractValidator<Voyager.Contracts.Driver.UpdateLocation>
{
  public UpdateLocationValidator()
  {
    // NotNull alone let an out-of-range coordinate through to SQL Server's geography column,
    // which rejects it at UPDATE time as a 500 rather than as bad input.
    RuleFor(x => x.Location).ValidCoordinate();
  }
}
