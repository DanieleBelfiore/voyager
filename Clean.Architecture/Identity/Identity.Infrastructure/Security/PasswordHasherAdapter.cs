using Identity.Application.Ports;
using Microsoft.AspNetCore.Identity;

namespace Identity.Infrastructure.Security;

/// <summary>
/// Wraps ASP.NET Core Identity's standalone PBKDF2 hasher — a pure algorithm implementation
/// that doesn't require UserManager/EF stores, so it can back Application's port without
/// pulling the rest of ASP.NET Core Identity's machinery into the picture.
/// </summary>
public class PasswordHasherAdapter : IPasswordHasher
{
  private readonly PasswordHasher<object> _hasher = new();

  public string Hash(string password) => _hasher.HashPassword(null, password);

  public bool Verify(string passwordHash, string providedPassword) =>
    _hasher.VerifyHashedPassword(null, passwordHash, providedPassword) != PasswordVerificationResult.Failed;
}
