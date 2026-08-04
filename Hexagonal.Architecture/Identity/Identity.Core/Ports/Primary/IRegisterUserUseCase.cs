using Hikyaku;

namespace Identity.Core.Ports.Primary;

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

public interface IRegisterUserUseCase : IRequestHandler<RegisterUser>;
