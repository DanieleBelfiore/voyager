using System;
using Hub.Module.Middlewares;
using Hub.Module.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.IO.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using StackExchange.Redis;

namespace Hub.Module.DependencyInjection;

/// <summary>
/// The module's only public surface. VoyagerHub itself stays internal — Host never names it
/// directly, it just calls AddHubModule then MapHubModule.
/// </summary>
public static class HubModuleExtensions
{
  public static IServiceCollection AddHubModule(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddSignalR(options =>
    {
      options.MaximumReceiveMessageSize = 32 * 1024;
      options.ClientTimeoutInterval = TimeSpan.FromSeconds(configuration.GetValue<double>("SignalR:ClientTimeoutSeconds"));
      options.KeepAliveInterval = TimeSpan.FromSeconds(configuration.GetValue<double>("SignalR:KeepAliveSeconds"));
      options.EnableDetailedErrors = true;
    }).AddNewtonsoftJsonProtocol(options =>
    {
      options.PayloadSerializerSettings.Converters.Add(new StringEnumConverter());
      // Without this, a Point argument/payload (UpdateDriverLocation, SendToRiderNewDriverLocation)
      // serializes as NTS's internal Coordinate/CoordinateSequence/Factory graph instead of GeoJSON,
      // and won't round-trip back into a Point on the other end. Same converter the MVC pipeline
      // in Host/Program.cs already registers for controller request/response bodies.
      options.PayloadSerializerSettings.Converters.Add(new GeometryConverter());
      options.PayloadSerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
      options.PayloadSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
      options.PayloadSerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
      options.PayloadSerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
      options.PayloadSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
    // A single process is fine for this exercise's scale, but the backplane is what makes
    // horizontal scaling actually work for SignalR: without it, a client connected to one
    // instance never receives a group message published from another, since group membership
    // and Clients.Group(...) dispatch are both in-memory and per-instance.
    }).AddStackExchangeRedis(options =>
    {
      // Same connect options the cache side already uses (see CacheExtensions):
      // AbortOnConnectFail=false so an instance still starts when Redis is briefly
      // unreachable and reconnects on its own, rather than throwing at startup.
      options.Configuration = new ConfigurationOptions
      {
        EndPoints = { configuration["Redis:ConnectionString"] },
        AbortOnConnectFail = false,
        ConnectTimeout = 6000,
        SyncTimeout = 6000,
        ConnectRetry = 3
      };
    });

    // Identity/Driver/Ride each also call AddHealthChecks() — safe to call again here, it's the
    // same registration composing onto one HealthCheckService — but this module shouldn't rely
    // on another module having run first just to make Host's /health and /ready resolve at all.
    services.AddHealthChecks().AddRedis(configuration["Redis:ConnectionString"], "redis", tags: ["ready"]);

    services.AddTransient<QueryStringTokenMiddleware>();

    return services;
  }

  /// <summary>Must run before UseAuthentication — pulls the SignalR access_token query param into the Authorization header.</summary>
  public static WebApplication UseHubModule(this WebApplication app)
  {
    app.UseMiddleware<QueryStringTokenMiddleware>();

    return app;
  }

  /// <summary>Endpoint mapping — call alongside MapControllers at the end of the pipeline.</summary>
  public static WebApplication MapHubModule(this WebApplication app)
  {
    app.MapHub<VoyagerHub>("/voyagerhub");

    return app;
  }
}
