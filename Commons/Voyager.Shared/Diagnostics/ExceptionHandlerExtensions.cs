using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Voyager.Shared.Diagnostics;

/// <summary>
/// Maps domain exceptions to HTTP status codes for every service that references this project
/// (Clean, Hexagonal, Vertical Slice, Modular Monolith).
///
/// Only plain BCL exception types are matched, deliberately: the Domain/Application/Core layers
/// that raise them are architecturally barred from referencing Voyager.Shared, so there is no
/// shared custom exception hierarchy for them to throw. Plugin.Microservices.CQRS is
/// self-contained by design and keeps its own copy over its own Common.Core exception types.
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
        UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "forbidden"),
        KeyNotFoundException => (StatusCodes.Status404NotFound, "not_found"),
        InvalidOperationException => (StatusCodes.Status409Conflict, "conflict"),
        _ => (StatusCodes.Status500InternalServerError, "internal_server_error")
      };

      context.Response.StatusCode = statusCode;
      context.Response.ContentType = "application/json";
      await context.Response.WriteAsJsonAsync(new { error });
    }));

    return app;
  }
}
