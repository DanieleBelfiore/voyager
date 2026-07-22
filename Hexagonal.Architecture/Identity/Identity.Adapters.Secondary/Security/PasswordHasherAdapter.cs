using Identity.Core.Ports.Secondary;
using Microsoft.AspNetCore.Identity;

namespace Identity.Adapters.Secondary.Security;

public class PasswordHasherAdapter : IPasswordHasher
{
  private readonly PasswordHasher<object> _hasher = new();

  public string Hash(string password) => _hasher.HashPassword(null, password);

  public bool Verify(string passwordHash, string providedPassword) =>
    _hasher.VerifyHashedPassword(null, passwordHash, providedPassword) != PasswordVerificationResult.Failed;
}
