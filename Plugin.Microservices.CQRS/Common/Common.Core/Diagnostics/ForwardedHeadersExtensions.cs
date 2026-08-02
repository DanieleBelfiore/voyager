using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Core.Diagnostics;

/// <summary>
/// Reconstructs the original client's IP and scheme from a reverse proxy's forwarded headers.
///
/// The IP half is what rate limiting depends on: RateLimitingExtensions partitions anonymous
/// callers by <c>HttpContext.Connection.RemoteIpAddress</c>, and behind a proxy that address is
/// the proxy's, so every anonymous caller shares one bucket and a single client can exhaust the
/// limit for everyone. Only XForwardedProto was enabled before, so XForwardedFor was discarded
/// and the partition key never reflected the real caller.
/// </summary>
public static class ForwardedHeadersExtensions
{
  public static void AddForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
  {
    services.Configure<ForwardedHeadersOptions>(options =>
    {
      options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

      // X-Forwarded-For is caller-supplied. Honouring it from an untrusted peer would let a
      // client name its own rate-limit partition just by setting the header, which is strictly
      // worse than the shared-proxy-IP bucket this is meant to fix. So: trust nothing unless
      // configured. With no ForwardedHeaders section the headers are ignored and the real
      // socket address is used — correct for the docker-compose setup, where the services are
      // published directly and no proxy exists.
      options.KnownProxies.Clear();
      options.KnownIPNetworks.Clear();

      foreach (var proxy in configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
        if (IPAddress.TryParse(proxy, out var address))
          options.KnownProxies.Add(address);

      foreach (var network in configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [])
      {
        var parts = network.Split('/');
        if (parts.Length == 2 && IPAddress.TryParse(parts[0], out var prefix) && int.TryParse(parts[1], out var length))
          options.KnownIPNetworks.Add(new System.Net.IPNetwork(prefix, length));
      }

      // One hop by default: with a longer limit a client can prepend its own X-Forwarded-For
      // entries and have the middleware walk past the proxy's appended value to a forged one.
      options.ForwardLimit = configuration.GetValue<int?>("ForwardedHeaders:ForwardLimit") ?? 1;
    });
  }
}
