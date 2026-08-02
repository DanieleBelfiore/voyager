using System;

namespace Identity.Domain.Entities;

/// <summary>
/// Owns its own persistence — unlike the Plugin.Microservices.CQRS variant, this is a plain
/// entity, not a subclass of ASP.NET Core Identity's IdentityUser&lt;Guid&gt;. Password
/// hashing/verification and OpenIddict token issuance stay out of Domain; see
/// Identity.Application.Ports.IPasswordHasher and Identity.Api's AuthController.
/// </summary>
public class User
{
  public Guid Id { get; private set; }
  public string Email { get; private set; }
  public string FirstName { get; private set; }
  public string LastName { get; private set; }
  public string PhoneNumber { get; private set; }
  public string PasswordHash { get; private set; }
  public bool IsDriver { get; private set; }
  public double Ratings { get; private set; }
  public int RatingsCount { get; private set; }
  public DateTime Created { get; private set; } = DateTime.UtcNow;
  public DateTime Modified { get; private set; } = DateTime.UtcNow;
  public DateTime LastLogin { get; private set; } = DateTime.UtcNow;

  private User()
  {
    // EF Core
  }

  public User(Guid id, string email, string firstName, string lastName, string phoneNumber, string passwordHash, bool isDriver)
  {
    Id = id;
    Email = email;
    FirstName = firstName;
    LastName = lastName;
    PhoneNumber = phoneNumber;
    PasswordHash = passwordHash;
    IsDriver = isDriver;
  }

  public void RecordLogin()
  {
    LastLogin = DateTime.UtcNow;
  }

}
