using Identity.Application.Dtos;
using MediatR;

namespace Identity.Application.CQRS.Queries;

public class AuthenticateUser : IRequest<AuthenticateUserResult>
{
  public string Username { get; set; }
  public string Password { get; set; }
}
