using System;

namespace Voyager.Errors;

/// <summary>
/// The caller sent a value the domain rejects outright — out of range, malformed, nonsensical.
/// Maps to HTTP 400.
///
/// Distinct from <see cref="ConflictException"/>: a conflict means the request would have been
/// fine against a different state of the resource, while this one is never valid. Distinct from
/// FluentValidation's ValidationException only in where it is raised — validators run at the
/// edge and produce nicer per-field messages, but the invariant still has to hold in the domain,
/// which cannot reference FluentValidation.
/// </summary>
public class InvalidInputException : Exception
{
  public InvalidInputException()
  {
  }

  public InvalidInputException(string message) : base(message)
  {
  }

  public InvalidInputException(string message, Exception innerException) : base(message, innerException)
  {
  }
}
