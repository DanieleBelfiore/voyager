using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Hub.Middlewares
{
  public class QueryStringTokenMiddleware : IMiddleware
  {
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
      var accessToken = context.Request.Query["access_token"];
      var path = context.Request.Path;

      if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/voyagerhub"))
        context.Request.Headers.Authorization = $"Bearer {accessToken}";

      await next(context);
    }
  }
}
