using FluentValidation;

namespace Driver.Api.Features.UpdateLocation;

public class UpdateLocationValidator : AbstractValidator<Voyager.Contracts.Driver.UpdateLocation>
{
  public UpdateLocationValidator()
  {
    RuleFor(x => x.Location).NotNull();
  }
}
