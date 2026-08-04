using System;
using Identity.Infrastructure.Messaging;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Repositories;
using Identity.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Voyager.Shared.Security;
using IUserRepository = Identity.Application.Ports.IUserRepository;
using IPasswordHasher = Identity.Application.Ports.IPasswordHasher;
using IDriverRegistration = Identity.Application.Ports.IDriverRegistration;

namespace Identity.Infrastructure.DependencyInjection;

public static class IdentityInfrastructureExtensions
{
  public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
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
