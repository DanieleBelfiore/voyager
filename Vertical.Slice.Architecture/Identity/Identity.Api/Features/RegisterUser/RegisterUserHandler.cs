using System;
using System.Threading;
using System.Threading.Tasks;
using Identity.Api.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Voyager.Errors;
using UserEntity = Identity.Api.Entities.User;

namespace Identity.Api.Features.RegisterUser;

/// <summary>
/// Format validation (name/email/password shape) runs in RegisterUserValidator through the
/// shared MediatR pipeline behavior — this handler only owns the invariant that can't be
/// checked without a database round-trip (email uniqueness), plus the actual write.
/// No IPasswordHasher port — PasswordHasher&lt;object&gt; is ASP.NET Core Identity's own
/// standalone hasher, taken as a direct dependency. No IDriverRegistration port either —
/// this handler just sends the shared AddDriver contract via IMediator, same as any other
/// cross-service call in this variant; Arbitrer routes it to Driver.
/// </summary>
public class RegisterUserHandler(IdentityDbContext db, PasswordHasher<object> passwordHasher, IMediator mediator) : IRequestHandler<RegisterUser>
{
  public async Task Handle(RegisterUser request, CancellationToken cancellationToken)
  {
    var email = request.Email.Trim();

    var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    if (existing != null)
      // A typed exception rather than a bare Exception("already_exist"): the controller decides
      // whether a message is safe to echo by type, and echoing every exception's message leaked
      // internal failure text to an unauthenticated caller.
      throw new ConflictException("already_exist");

    var user = new UserEntity(
      Guid.NewGuid(),
      email,
      request.FirstName.Trim(),
      request.LastName.Trim(),
      request.PhoneNumber?.Trim(),
      passwordHasher.HashPassword(null, request.Password),
      request.IsDriver);

    db.Users.Add(user);

    await db.SaveChangesAsync(cancellationToken);

    if (user.IsDriver)
      await RegisterAsDriverOrRollBackAsync(user, cancellationToken);
  }

  /// <summary>
  /// Creates the Driver row that puts this user into SearchBestDriver's candidate pool, and
  /// undoes the user itself if that fails.
  ///
  /// Without the compensation the two services could disagree permanently: the user existed but
  /// the driver did not, the caller was told registration failed, and retrying hit
  /// "already_exist" forever — leaving a driver who can never be matched to a ride. The stores
  /// live in different services so there is no transaction to lean on; removing the user is what
  /// makes the failure retryable.
  /// </summary>
  private async Task RegisterAsDriverOrRollBackAsync(UserEntity user, CancellationToken cancellationToken)
  {
    try
    {
      await mediator.Send(new Voyager.Contracts.Driver.AddDriver { DriverId = user.Id }, cancellationToken);
    }
    catch
    {
      db.Users.Remove(user);

      // Not wrapped in its own try: if the rollback itself fails there is nothing further to do
      // here, and swallowing it would report a clean failure while leaving the user behind.
      await db.SaveChangesAsync(CancellationToken.None);

      throw;
    }
  }
}
