using MediatR;

namespace Identity.Api.Features.AuthenticateUser;

public class AuthenticateUser : IRequest<AuthenticateUserResult>
{
  public string Username { get; set; }
  public string Password { get; set; }
}
