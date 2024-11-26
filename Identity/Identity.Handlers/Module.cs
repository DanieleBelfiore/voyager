using System;
using System.Composition;
using System.Reflection;
using Common.Core.Interfaces;
using Identity.Handlers.Interfaces;
using Identity.Handlers.Models;
using Identity.Handlers.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Identity.Handlers
{
  [Export(typeof(IModule))]
  public class Module : IModule
  {
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment hostingEnvironment)
    {
      services.AddDbContext<SQLMigrationContext>(options => options.UseSqlServer(configuration.GetConnectionString("IdentityContext")));
      services.AddDbContext<IdentityContext>((provider, options) =>
      {
        options.UseSqlServer(configuration.GetConnectionString("IdentityContext"));
        options.UseOpenIddict();
        options.AddInterceptors(provider.GetRequiredService<SlowQueryInterceptor>());
      });

      services.AddScoped<IIdentityContext>(provider => provider.GetService<IdentityContext>());

      services.AddScoped<SlowQueryInterceptor>();

      services.AddScoped<IUserManager, UserManagerService>();

      services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
    }

    public void OnStartup(IApplicationBuilder app)
    {
      using var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();

      try
      {
        var context = serviceScope.ServiceProvider.GetService<SQLMigrationContext>();
        using (context)
        {
          context?.Database.Migrate();
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
        Console.WriteLine(ex.StackTrace);
        throw;
      }
    }

    public void UseEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
  }
}
