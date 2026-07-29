using System;
using System.Threading;
using System.Threading.Tasks;
using Identity.Module.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserEntity = Identity.Module.Entities.User;

namespace Identity.Module.Features.RegisterUser;

internal class RegisterUserHandler(IdentityDbContext db, PasswordHasher<object> passwordHasher, IMediator mediator) : IRequestHandler<RegisterUser>
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
