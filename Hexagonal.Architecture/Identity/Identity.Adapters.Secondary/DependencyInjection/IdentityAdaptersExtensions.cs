using System;
using Identity.Adapters.Secondary.Messaging;
using Identity.Adapters.Secondary.Persistence;
using Identity.Adapters.Secondary.Repositories;
using Identity.Adapters.Secondary.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Voyager.Shared.Security;
using IUserRepository = Identity.Core.Ports.Secondary.IUserRepository;
using IPasswordHasher = Identity.Core.Ports.Secondary.IPasswordHasher;
using IDriverRegistration = Identity.Core.Ports.Secondary.IDriverRegistration;

namespace Identity.Adapters.Secondary.DependencyInjection;

public static class IdentityAdaptersExtensions
{
  public static IServiceCollection AddIdentitySecondaryAdapters(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddDbContext<IdentityDbContext>((provider, options) =>
    {
      options.UseSqlServer(configuration.GetConnectionString("IdentityContext"));
      options.UseOpenIddict();
      options.AddInterceptors(provider.GetRequiredService<SlowQueryInterceptor>());
    });

    services.AddScoped<SlowQueryInterceptor>();

    services.AddScoped<IUserRepository, UserRepository>();
    services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();
    services.AddScoped<IDriverRegistration, RemoteDriverRegistration>();

    return services;
  }

  public static void MigrateIdentityDatabase(this IServiceProvider services)
  {
    using var scope = services.CreateScope();
    using var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    context.Database.Migrate();

    // Without this the service cannot issue a token at all — see OpenIddictClientSeed.
    scope.ServiceProvider.SeedVoyagerApplicationAsync().GetAwaiter().GetResult();
  }
}
