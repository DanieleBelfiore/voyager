using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Common.Core.Diagnostics;

public static class StartupMigrationExtensions
{
  /// <summary>
  /// Registers <paramref name="migrate"/> to run once at startup after the server is listening,
  /// plus a "ready"-tagged health check that stays unhealthy until it completes. Call this on the
  /// builder instead of invoking Migrate() against the built app.
  /// </summary>
  public static IServiceCollection AddStartupMigration(this IServiceCollection services, Action<IServiceProvider> migrate)
  {
    services.AddSingleton<StartupMigrationState>();

    services.AddHostedService(sp => new StartupMigrationHostedService(
      sp,
      migrate,
      sp.GetRequiredService<StartupMigrationState>(),
      sp.GetRequiredService<ILogger<StartupMigrationHostedService>>()));

    services.AddHealthChecks().AddCheck<StartupMigrationHealthCheck>("database-migration", tags: ["ready"]);

    return services;
  }
}
