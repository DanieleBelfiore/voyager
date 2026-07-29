using System;
using System.Threading;
using System.Threading.Tasks;
using Identity.Api.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
      throw new Exception("already_exist");

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
      await mediator.Send(new Voyager.Contracts.Driver.AddDriver { DriverId = user.Id }, cancellationToken);
  }
}
