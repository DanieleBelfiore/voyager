using System.Composition;
using Common.Core.Interfaces;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ride.API;

[Export(typeof(IModule))]
[UsedImplicitly]
public class Module : IModule
{
  public void ConfigureServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment hostingEnvironment)
  {
  }

  public void OnStartup(IApplicationBuilder app)
  {
  }

  public void UseEndpoints(IEndpointRouteBuilder endpoints)
  {
  }
}
