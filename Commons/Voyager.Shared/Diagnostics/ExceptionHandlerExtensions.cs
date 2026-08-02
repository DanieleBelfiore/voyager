using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Voyager.Errors;

namespace Voyager.Shared.Diagnostics;

/// <summary>
/// Maps domain exceptions to HTTP status codes for every service that references this project
/// (Clean, Hexagonal, Vertical Slice, Modular Monolith). Plugin.Microservices.CQRS is
/// self-contained by design and keeps its own copy over its own Common.Core exception types.
///
/// Every arm matches a type that can only be thrown deliberately. The previous version mapped
/// InvalidOperationException to 409, which the BCL itself raises for unrelated faults — LINQ's
/// First() on an empty sequence, EF Core's "no provider configured", DI resolution failures —
/// so a genuine outage surfaced as a business conflict and never paged anyone. Domain layers
/// now throw Voyager.Errors.ConflictException instead; that project carries no dependencies at
/// all, so a pure Domain/Core layer can reference it without taking on a framework.
/// </summary>
public static class ExceptionHandlerExtensions
{
  public static WebApplication UseDomainExceptionHandler(this WebApplication app)
  {
    if (app.Environment.IsDevelopment())
    {
      app.UseDeveloperExceptionPage();

      return app;
    }

    // Do not leak stack traces/paths outside Development: a generic response body, with domain
    // exceptions mapped to their own status instead of falling through to a blanket 500.
    app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
    {
      var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

      var (statusCode, error) = exception switch
      {
        ValidationException => (StatusCodes.Status400BadRequest, "validation_failed"),
        InvalidInputException => (StatusCodes.Status400BadRequest, "invalid_input"),
        UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "forbidden"),
        KeyNotFoundException => (StatusCodes.Status404NotFound, "not_found"),
        ConflictException => (StatusCodes.Status409Conflict, "conflict"),
        _ => (StatusCodes.Status500InternalServerError, "internal_server_error")
      };

      context.Response.StatusCode = statusCode;
      context.Response.ContentType = "application/json";

      // ValidationException is the one case that carries a body beyond the error code: its
      // messages are the validator's own rule text about the caller's own input, so echoing
      // them discloses nothing the caller didn't send. Without this the request came back as a
      // bare 500 and the caller had no way to learn which field was wrong.
      if (exception is ValidationException validation)
      {
        await context.Response.WriteAsJsonAsync(new
        {
          error,
          details = validation.Errors
            .GroupBy(f => f.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray())
        });

        return;
      }

      await context.Response.WriteAsJsonAsync(new { error });
    }));

    return app;
  }
}
