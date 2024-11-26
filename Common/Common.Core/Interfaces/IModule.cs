using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Common.Core.Interfaces
{
  public interface IModule
  {
    void ConfigureServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment hostingEnvironment);
    void OnStartup(IApplicationBuilder app);
    void UseEndpoints(IEndpointRouteBuilder endpoints);
  }
}
