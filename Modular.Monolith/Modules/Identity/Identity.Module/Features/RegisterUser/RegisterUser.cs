using Hikyaku;

namespace Identity.Module.Features.RegisterUser;

/// <summary>Public — bound directly from the request body by RegisterUserController, so it can't be internal (CS0050).</summary>
public class RegisterUser : IRequest
{
  public string FirstName { get; set; }
  public string LastName { get; set; }
  public string Email { get; set; }
  public string Password { get; set; }
  public string ConfirmPassword { get; set; }
  public bool IsDriver { get; set; }
  public string PhoneNumber { get; set; }
}
