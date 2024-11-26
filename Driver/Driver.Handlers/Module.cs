using System;
using System.Composition;
using System.Data;
using System.Reflection;
using Common.Core.Interfaces;
using Driver.Handlers.Interfaces;
using Driver.Handlers.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Driver.Handlers
{
  [Export(typeof(IModule))]
  public class Module : IModule
  {
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment hostingEnvironment)
    {
      services.AddDbContext<SQLMigrationContext>(options => options.UseSqlServer(configuration.GetConnectionString("DriverContext"), a => a.UseNetTopologySuite()));
      services.AddDbContext<DriverContext>((provider, options) =>
      {
        options.UseSqlServer(configuration.GetConnectionString("DriverContext"), a => a.UseNetTopologySuite());
        options.AddInterceptors(provider.GetRequiredService<SlowQueryInterceptor>());
      });

      services.AddScoped<IDriverContext>(provider => provider.GetService<DriverContext>());

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
