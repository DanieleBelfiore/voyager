using System;

namespace Common.Core.Exceptions;

/// <summary>Requested resource doesn't exist — mapped to 404 by the host's exception handler
/// middleware, instead of falling through to the generic 500 response.</summary>
public class NotFoundException(string message) : Exception(message);
