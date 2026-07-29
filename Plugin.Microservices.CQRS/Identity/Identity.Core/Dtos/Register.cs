namespace Identity.Core.Dtos
{
  public class Register
  {
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string ConfirmPassword { get; set; }
    public bool IsDriver { get; set; }
    public string PhoneNumber { get; set; }
  }
}
