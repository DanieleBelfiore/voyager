using System;
using Hub.Module.Middlewares;
using Hub.Module.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Hub.Module.DependencyInjection;

/// <summary>
/// The module's only public surface. VoyagerHub itself stays internal — Host never names it
/// directly, it just calls AddHubModule then MapHubModule.
/// </summary>
public static class HubModuleExtensions
{
  public static IServiceCollection AddHubModule(this IServiceCollection services)
  {
    services.AddSignalR(options =>
    {
      options.MaximumReceiveMessageSize = 32 * 1024;
      options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
      options.KeepAliveInterval = TimeSpan.FromSeconds(15);
      options.EnableDetailedErrors = true;
    }).AddNewtonsoftJsonProtocol(options =>
    {
      options.PayloadSerializerSettings.Converters.Add(new StringEnumConverter());
      options.PayloadSerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
      options.PayloadSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
      options.PayloadSerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
      options.PayloadSerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
      options.PayloadSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
    });

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
