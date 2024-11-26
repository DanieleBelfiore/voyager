using System;
using System.Composition;
using System.Reflection;
using Common.Core.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ride.Handlers.Interfaces;
using Ride.Handlers.Models;

namespace Ride.Handlers
{
  [Export(typeof(IModule))]
  public class Module : IModule
  {
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment hostingEnvironment)
    {
      services.AddDbContext<SQLMigrationContext>(options => options.UseSqlServer(configuration.GetConnectionString("RideContext"), a => a.UseNetTopologySuite()));
      services.AddDbContext<RideContext>((provider, options) =>
      {
        options.UseSqlServer(configuration.GetConnectionString("RideContext"), a => a.UseNetTopologySuite());
        options.AddInterceptors(provider.GetRequiredService<SlowQueryInterceptor>());
      });

      services.AddScoped<IRideContext>(provider => provider.GetService<RideContext>());

      services.AddScoped<SlowQueryInterceptor>();

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
