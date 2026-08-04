using System;
using System.Threading;
using System.Threading.Tasks;
using Identity.Module.Persistence;
using Hikyaku;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Voyager.Errors;
using UserEntity = Identity.Module.Entities.User;

namespace Identity.Module.Features.RegisterUser;

internal class RegisterUserHandler(IdentityDbContext db, PasswordHasher<object> passwordHasher, IHikyaku mediator) : IRequestHandler<RegisterUser>
{
  public async Task Handle(RegisterUser request, CancellationToken cancellationToken)
  {
    var email = request.Email.Trim();

    var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    if (existing != null)
      // A typed exception rather than a bare Exception("already_exist"): the controller decides
      // whether a message is safe to echo by type, so it can no longer be fooled into returning
      // the text of an unrelated fault by string-matching on the message.
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
  /// Without the compensation the two modules could disagree permanently: the user existed but
  /// the driver did not, the caller was told registration failed, and retrying hit
  /// "already_exist" forever — leaving a driver who can never be matched to a ride. The modules
  /// own separate DbContexts even in one process, so there is no ambient transaction across the
  /// two writes; removing the user is what makes the failure retryable.
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
