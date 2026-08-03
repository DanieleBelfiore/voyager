using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Identity.Core.Domain;
using Identity.Core.Ports.Secondary;
using Identity.Core.Ports.Primary;
using Voyager.Errors;

namespace Identity.Core.UseCases;

public partial class RegisterUserUseCase(IUserRepository repository, IPasswordHasher passwordHasher, IDriverRegistration driverRegistration) : IRegisterUserUseCase
{
  public async Task Handle(RegisterUser request, CancellationToken cancellationToken)
  {
    // Typed exceptions rather than bare Exception("code"): the controller has to be able to tell
    // an expected rule violation (safe to report verbatim — it describes the caller's own input)
    // from an unexpected fault, whose message may carry internal detail and must not be echoed.
    if (string.IsNullOrWhiteSpace(request.FirstName))
      throw new InvalidInputException("first_name_required");

    if (string.IsNullOrWhiteSpace(request.LastName))
      throw new InvalidInputException("last_name_required");

    if (request.Email == null || !EmailPattern().IsMatch(request.Email))
      throw new InvalidInputException("email_required");

    if (request.Password == null || request.Password.Length < 8)
      throw new InvalidInputException("password_string_length");

    if (request.Password != request.ConfirmPassword)
      throw new InvalidInputException("confirm_password_not_matching");

    var email = request.Email.Trim();

    var existing = await repository.GetByEmailAsync(email, cancellationToken);
    if (existing != null)
      throw new ConflictException("already_exist");

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
      await RegisterAsDriverOrRollBackAsync(user, cancellationToken);
  }

  /// <summary>
  /// Creates the Driver row that puts this user into SearchBestDriver's candidate pool, and
  /// undoes the user itself if that fails.
  ///
  /// Without the compensation the two stores could disagree permanently: the user existed but
  /// the driver did not, the caller was told registration failed, and retrying hit
  /// "already_exist" forever — leaving a driver who can never be matched to a ride. The stores
  /// live in different services so there is no transaction to lean on; removing the user is what
  /// makes the failure retryable.
  /// </summary>
  private async Task RegisterAsDriverOrRollBackAsync(User user, CancellationToken cancellationToken)
  {
    try
    {
      await driverRegistration.RegisterAsDriverAsync(user.Id, cancellationToken);
    }
    catch
    {
      repository.Remove(user);

      // Not inside the catch's own try: if the rollback itself fails there is nothing further to
      // do here, and swallowing it would report a clean failure while leaving the user behind.
      await repository.SaveChangesAsync(CancellationToken.None);

      throw;
    }
  }

  [GeneratedRegex(@"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z", RegexOptions.IgnoreCase, "it-IT")]
  private static partial Regex EmailPattern();
}
