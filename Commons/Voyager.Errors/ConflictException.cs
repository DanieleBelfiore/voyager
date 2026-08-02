using System;

namespace Voyager.Errors;

/// <summary>
/// The request is well-formed and authorized, but conflicts with the current state of the
/// resource — a ride already in progress, a status transition that isn't legal from where the
/// entity currently is. Maps to HTTP 409.
///
/// This exists because the previous signal was <see cref="InvalidOperationException"/>, which
/// the BCL itself throws for unrelated faults (LINQ's First() on an empty sequence, EF Core's
/// "no database provider configured", DI resolution failures). Mapping that type to 409 turned
/// genuine 500s into business conflicts and hid real outages. A dedicated type can only be
/// thrown deliberately.
/// </summary>
public class ConflictException : Exception
{
  public ConflictException()
  {
  }

  public ConflictException(string message) : base(message)
  {
  }

  public ConflictException(string message, Exception innerException) : base(message, innerException)
  {
  }
}
