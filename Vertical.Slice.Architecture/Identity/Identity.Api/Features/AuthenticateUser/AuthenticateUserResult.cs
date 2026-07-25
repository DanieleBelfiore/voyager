using System;

namespace Identity.Api.Features.AuthenticateUser;

public class AuthenticateUserResult
{
  public bool Succeeded { get; set; }
  public string ErrorDescription { get; set; }
  public Guid UserId { get; set; }
  public string Email { get; set; }
  public bool IsDriver { get; set; }
}
