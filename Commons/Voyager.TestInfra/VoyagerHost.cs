using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Voyager.TestInfra;

/// <summary>
/// A variant's real host, booted in-process against the test containers. Nothing about the
/// application's own composition is replaced: the only thing injected is configuration, so what
/// the tests exercise is the pipeline as it ships — middleware, auth, EF, mediator and all.
/// </summary>
public class VoyagerHost<TProgram>(
  IDictionary<string, string> configuration,
  Action<IServiceCollection> configureServices = null)
  : WebApplicationFactory<TProgram> where TProgram : class
{
  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    // Production, not the Development default WebApplicationFactory picks. In Development the
    // pipeline installs the developer exception page, which answers 500 with a stack trace for
    // everything — so the ProblemDetails mapping that turns KeyNotFoundException into 404 and
    // ConflictException into 409 is never exercised, and a test asserting on status codes is
    // asserting on behaviour no deployment has. VoyagerContainers.Configuration sets
    // Identity:UseDevelopmentCertificates for the same reason: that flag exists precisely because
    // the compose stack also runs as Production.
    builder.UseEnvironment(Environments.Production);

    builder.ConfigureAppConfiguration(cfg => cfg.AddInMemoryCollection(
      configuration.Select(entry => new KeyValuePair<string, string>(entry.Key, entry.Value))));

    if (configureServices != null)
      builder.ConfigureTestServices(configureServices);
  }

  /// <summary>
  /// Forces the host to build. <c>WebApplicationFactory</c> is lazy, and a fixture that hands out
  /// clients needs startup work — migrations above all — to have already run.
  /// </summary>
  public VoyagerHost<TProgram> Start()
  {
    _ = Services;

    return this;
  }
}
