using FluentValidation;

namespace Driver.Module.Features.UpdateLocation;

internal class UpdateLocationValidator : AbstractValidator<Voyager.Contracts.Driver.UpdateLocation>
{
  public UpdateLocationValidator()
  {
    RuleFor(x => x.Location).NotNull();
  }
}
