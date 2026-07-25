using FluentValidation;

namespace Driver.Module.Features.UpdateAvailability;

internal class UpdateAvailabilityValidator : AbstractValidator<UpdateAvailability>
{
  public UpdateAvailabilityValidator()
  {
    RuleFor(x => x.Status).IsInEnum();
  }
}
