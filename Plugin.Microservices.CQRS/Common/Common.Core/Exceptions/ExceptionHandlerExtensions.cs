using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
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
      var (statusCode, error) = context.Features.Get<IExceptionHandlerFeature>()?.Error switch
      {
        InvalidInputException => (StatusCodes.Status400BadRequest, "invalid_input"),
        UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "forbidden"),
        NotFoundException => (StatusCodes.Status404NotFound, "not_found"),
        ConflictException => (StatusCodes.Status409Conflict, "conflict"),
        _ => (StatusCodes.Status500InternalServerError, "internal_server_error")
      };

      context.Response.StatusCode = statusCode;
      context.Response.ContentType = "application/json";
      await context.Response.WriteAsJsonAsync(new { error });
    }));

    return app;
  }
}
