using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Identity.Application.Ports;
using Identity.Domain.Entities;
using MediatR;

namespace Identity.Application.CQRS.Commands;

/// <summary>
/// Same validation and error messages as the Plugin.Microservices.CQRS variant's
/// UsersController.Register — moved here so the controller is thin protocol glue and this
/// logic is testable without ASP.NET Core.
/// </summary>
public partial class RegisterUserHandler(IUserRepository repository, IPasswordHasher passwordHasher, IDriverRegistration driverRegistration) : IRequestHandler<RegisterUser>
{
  public async Task Handle(RegisterUser request, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(request.FirstName))
      throw new Exception("first_name_required");

    if (string.IsNullOrWhiteSpace(request.LastName))
      throw new Exception("last_name_required");

    if (request.Email == null || !EmailPattern().IsMatch(request.Email))
      throw new Exception("email_required");

    if (request.Password == null || request.Password.Length < 8)
      throw new Exception("password_string_length");

    if (request.Password != request.ConfirmPassword)
      throw new Exception("confirm_password_not_matching");

    var email = request.Email.Trim();

    var existing = await repository.GetByEmailAsync(email, cancellationToken);
    if (existing != null)
      throw new Exception("already_exist");

    var user = new User(
      Guid.NewGuid(),
      email,
      request.FirstName.Trim(),
      request.LastName.Trim(),
      request.PhoneNumber?.Trim(),
      passwordHasher.Hash(request.Password),
      request.IsDriver);

    repository.Add(user);

    await repository.SaveChangesAsync(cancellationToken);

    if (user.IsDriver)
      await driverRegistration.RegisterAsDriverAsync(user.Id, cancellationToken);
  }

  [GeneratedRegex(@"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z", RegexOptions.IgnoreCase, "it-IT")]
  private static partial Regex EmailPattern();
}
