using System;

namespace Identity.Module.Entities;

internal class User
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

  // RatingsCount is self-tracked here (incremented once per call) rather than passed in by the
  // caller — the caller previously supplied "rides completed", which isn't the same number as
  // "ratings actually received" (a completed ride isn't necessarily rated).
  public void UpdateRating(int rating)
  {
    RatingsCount++;
    Ratings = RatingsCount <= 1 ? rating : (Ratings * (RatingsCount - 1) + rating) / (double)RatingsCount;
    Modified = DateTime.UtcNow;
  }
}
