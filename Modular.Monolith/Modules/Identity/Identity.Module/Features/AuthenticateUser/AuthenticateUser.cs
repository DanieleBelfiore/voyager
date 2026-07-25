using MediatR;

namespace Identity.Module.Features.AuthenticateUser;

internal class AuthenticateUser : IRequest<AuthenticateUserResult>
{
  public string Username { get; set; }
  public string Password { get; set; }
}
