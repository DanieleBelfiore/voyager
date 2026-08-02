using System;

namespace Common.Core.Exceptions;

/// <summary>Request conflicts with the resource's current state (e.g. an invalid status
/// transition, or a uniqueness rule) — mapped to 409 by the host's exception handler
/// middleware, instead of falling through to the generic 500 response.</summary>
public class ConflictException(string message) : Exception(message);
