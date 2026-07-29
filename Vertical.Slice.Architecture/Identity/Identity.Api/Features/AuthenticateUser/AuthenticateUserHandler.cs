using System.Threading;
using System.Threading.Tasks;
using Identity.Api.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Features.AuthenticateUser;

public class AuthenticateUserHandler(IdentityDbContext db, PasswordHasher<object> passwordHasher) : IRequestHandler<AuthenticateUser, AuthenticateUserResult>
{
  private const string InvalidCredentialsMessage = "The username/password couple is invalid.";

  public async Task<AuthenticateUserResult> Handle(AuthenticateUser request, CancellationToken cancellationToken)
  {
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Username, cancellationToken);
    if (user == null || passwordHasher.VerifyHashedPassword(null, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
      return new AuthenticateUserResult { Succeeded = false, ErrorDescription = InvalidCredentialsMessage };

    user.RecordLogin();

    await db.SaveChangesAsync(cancellationToken);

    return new AuthenticateUserResult
    {
      Succeeded = true,
      UserId = user.Id,
      Email = user.Email,
      IsDriver = user.IsDriver
    };
  }
}
