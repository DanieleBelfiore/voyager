using MediatR;

namespace Identity.Api.Features.RegisterUser;

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
