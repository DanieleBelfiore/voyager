using System;

namespace Identity.Api.Entities;

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

  public void UpdateRating(int rating, int rides)
  {
    Ratings = (Ratings + rating) / rides;
    Modified = DateTime.UtcNow;
  }
}
