using FluentValidation;

namespace Identity.Module.Features.RegisterUser;

internal class RegisterUserValidator : AbstractValidator<RegisterUser>
{
  public RegisterUserValidator()
  {
    RuleFor(x => x.FirstName).NotEmpty().WithMessage("first_name_required");
    RuleFor(x => x.LastName).NotEmpty().WithMessage("last_name_required");
    RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("email_required");
    RuleFor(x => x.Password).MinimumLength(8).WithMessage("password_string_length");
    RuleFor(x => x.ConfirmPassword).Equal(x => x.Password).WithMessage("confirm_password_not_matching");
  }
}
