using System;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Core.RateLimiting
{
  public static class RateLimitingExtensions
  {
    public static void AddCustomRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
      var config = configuration.GetSection("RateLimiting").Get<RateLimitingConfiguration>();

      services.AddRateLimiter(options =>
      {
        // Default policy
        options.AddFixedWindowLimiter("fixed", opt =>
        {
          opt.PermitLimit = config.PermitLimit;
          opt.Window = TimeSpan.FromSeconds(config.Window);
          opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
          opt.QueueLimit = config.QueueLimit;
        });

        // Specific policies per endpoint
        foreach (var limit in config.EndpointLimits)
        {
          options.AddPolicy(limit.Key, context =>
          {
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.User.Identity?.Name ?? context.Request.Headers.UserAgent.ToString(),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                  PermitLimit = limit.Value.PermitLimit,
                  Window = TimeSpan.FromSeconds(limit.Value.Window),
                  QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                  QueueLimit = config.QueueLimit
                });
          });
        }

        // OnRejected handler
        options.OnRejected = async (context, token) =>
        {
          context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

          if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
          {
            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
              error = "too_many_requests",
              message = "Too many requests. Please try again later.",
              retryAfter = retryAfter.TotalSeconds
            }, token);
          }
        };
      });
    }
  }
}
