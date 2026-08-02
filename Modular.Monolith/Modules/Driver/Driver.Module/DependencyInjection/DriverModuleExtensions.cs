using System;
using Driver.Module.Features.SearchBestDriver;
using Driver.Module.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Voyager.Shared.Cache;

namespace Driver.Module.DependencyInjection;

/// <summary>
/// The module's only public surface besides its controllers. Host calls AddDriverModule during
/// composition and MigrateDriverDatabase at startup; MediatR/FluentValidation registration is
/// centralized in Host (see Host/Program.cs) since there's one shared mediator across all
/// modules in this single process — this method only wires this module's own infrastructure.
/// </summary>
public static class DriverModuleExtensions
{
  public static IServiceCollection AddDriverModule(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddDbContext<DriverDbContext>((provider, options) =>
    {
      options.UseSqlServer(configuration.GetConnectionString("DriverContext"), a => a.UseNetTopologySuite());
      options.AddInterceptors(provider.GetRequiredService<SlowQueryInterceptor>());
    });

    services.AddScoped<SlowQueryInterceptor>();

    services.AddHealthChecks().AddDbContextCheck<DriverDbContext>("driver-database", tags: ["ready"]);

    services.AddRedisCache(configuration);

    services.Configure<MatchingWeights>(configuration);

    return services;
  }

  public static void MigrateDriverDatabase(this IServiceProvider services)
  {
    using var scope = services.CreateScope();
    using var context = scope.ServiceProvider.GetRequiredService<DriverDbContext>();
    context.Database.Migrate();
  }
}
