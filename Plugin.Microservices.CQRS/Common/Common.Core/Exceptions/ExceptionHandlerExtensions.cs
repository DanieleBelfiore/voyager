using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Common.Core.Exceptions;

/// <summary>
/// Maps domain exceptions to HTTP status codes for every service in this variant.
///
/// This variant is self-contained on purpose — it holds no reference to the repo-level
/// Commons/ projects — so it matches on its own NotFoundException/ConflictException rather than
/// the plain BCL types the other variants are forced to use.
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
      var (statusCode, error) = Map(context.Features.Get<IExceptionHandlerFeature>()?.Error);

      context.Response.StatusCode = statusCode;
      context.Response.ContentType = "application/json";
      await context.Response.WriteAsJsonAsync(new { error });
    }));

    return app;
  }

  /// <summary>
  /// The exception-to-status table, kept as a pure function so it is assertable without hosting
  /// a request pipeline. Anything not listed is deliberately a 500: an unmapped exception is an
  /// unhandled fault, and reporting it as a client error would hide a real outage.
  /// </summary>
  public static (int StatusCode, string Error) Map(Exception exception)
  {
    return exception switch
    {
      InvalidInputException => (StatusCodes.Status400BadRequest, "invalid_input"),
      UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "forbidden"),
      NotFoundException => (StatusCodes.Status404NotFound, "not_found"),
      ConflictException => (StatusCodes.Status409Conflict, "conflict"),
      // Ride carries a RowVersion concurrency token, so two writers racing the same ride make EF
      // Core raise this instead of silently overwriting. Nothing caught it, so losing that race
      // surfaced as a blanket 500. It is a conflict like any other, but under its own code
      // because the client action differs: a lost race is safe to retry, an invalid status
      // transition is not. Listed after ConflictException only for readability — the two types
      // are unrelated, so the arm order carries no meaning here.
      DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "concurrent_modification"),
      _ => (StatusCodes.Status500InternalServerError, "internal_server_error")
    };
  }
}
