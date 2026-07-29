using System.Threading;
using System.Threading.Tasks;
using Identity.Core.Dtos;
using Identity.Core.Ports.Secondary;
using Identity.Core.Ports.Primary;

namespace Identity.Core.UseCases;

public class AuthenticateUserUseCase(IUserRepository repository, IPasswordHasher passwordHasher) : IAuthenticateUserUseCase
{
  private const string InvalidCredentialsMessage = "The username/password couple is invalid.";

  public async Task<AuthenticateUserResult> Handle(AuthenticateUser request, CancellationToken cancellationToken)
  {
    var user = await repository.GetByEmailAsync(request.Username, cancellationToken);
    if (user == null || !passwordHasher.Verify(user.PasswordHash, request.Password))
      return new AuthenticateUserResult { Succeeded = false, ErrorDescription = InvalidCredentialsMessage };

    user.RecordLogin();

    await repository.SaveChangesAsync(cancellationToken);

    return new AuthenticateUserResult
    {
      Succeeded = true,
      UserId = user.Id,
      Email = user.Email,
      IsDriver = user.IsDriver
    };
  }
}
