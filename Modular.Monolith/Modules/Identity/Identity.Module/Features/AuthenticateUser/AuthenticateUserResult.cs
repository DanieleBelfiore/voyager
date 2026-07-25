using System;

namespace Identity.Module.Features.AuthenticateUser;

internal class AuthenticateUserResult
{
  public bool Succeeded { get; set; }
  public string ErrorDescription { get; set; }
  public Guid UserId { get; set; }
  public string Email { get; set; }
  public bool IsDriver { get; set; }
}
