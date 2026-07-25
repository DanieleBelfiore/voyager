using FluentValidation;

namespace Driver.Api.Features.UpdateAvailability;

public class UpdateAvailabilityValidator : AbstractValidator<UpdateAvailability>
{
  public UpdateAvailabilityValidator()
  {
    RuleFor(x => x.Status).IsInEnum();
  }
}
