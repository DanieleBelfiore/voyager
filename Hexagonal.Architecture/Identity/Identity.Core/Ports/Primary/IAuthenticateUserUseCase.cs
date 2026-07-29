using Identity.Core.Dtos;
using MediatR;

namespace Identity.Core.Ports.Primary;

public class AuthenticateUser : IRequest<AuthenticateUserResult>
{
  public string Username { get; set; }
  public string Password { get; set; }
}

public interface IAuthenticateUserUseCase : IRequestHandler<AuthenticateUser, AuthenticateUserResult>;
