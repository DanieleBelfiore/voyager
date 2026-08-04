using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace Voyager.TestInfra;

/// <summary>
/// Points a host's outbound HTTP at another host's in-memory test server.
/// </summary>
/// <remarks>
/// In the service-per-process variants, Driver/Ride/Hub validate bearer tokens by talking to the
/// Identity service over HTTP — <c>SetIssuer(...)</c> plus <c>UseSystemNetHttp()</c>, so OpenIddict
/// fetches the issuer's discovery document and signing keys at runtime. Under
/// <c>WebApplicationFactory</c> nothing is listening on a real socket, so that lookup cannot
/// resolve and every authenticated request fails with 500 before reaching a controller.
///
/// Redirecting is what keeps the *shipped* validation wiring under test: the tests still exercise
/// remote discovery against the real Identity host, only the transport is the test server rather
/// than a TCP connection. Swapping the registration for <c>UseLocalServer()</c> instead would test
/// a configuration no deployment runs.
///
/// It has to be a <see cref="DelegatingHandler"/>, not a replacement primary handler: OpenIddict
/// post-configures its own client and throws outright on anything else — "Only instances of type
/// 'System.Net.Http.HttpClientHandler' can be used as primary HTTP handlers for the HTTP clients
/// managed by OpenIddict". So the primary is left alone and this sits in front of it, answering
/// every request itself and never delegating inward.
///
/// Applied to every named client rather than OpenIddict's specific one: the client name is an
/// implementation detail of the integration package, and these hosts make no other outbound calls.
/// </remarks>
public static class TestServerRouting
{
  public static void RouteOutboundHttpTo(this IServiceCollection services, Func<HttpMessageHandler> handler)
  {
    services.AddSingleton<IConfigureOptions<HttpClientFactoryOptions>>(new RouteAllClients(handler));
  }

  private class RouteAllClients(Func<HttpMessageHandler> handler) : IConfigureNamedOptions<HttpClientFactoryOptions>
  {
    public void Configure(HttpClientFactoryOptions options)
    {
      Configure(Options.DefaultName, options);
    }

    public void Configure(string name, HttpClientFactoryOptions options)
    {
      // Added last, so it ends up innermost — in front of the primary handler it replaces in
      // practice but never touches.
      options.HttpMessageHandlerBuilderActions.Add(builder =>
        builder.AdditionalHandlers.Add(new RedirectToTestServer(handler())));
    }
  }

  private class RedirectToTestServer(HttpMessageHandler target) : DelegatingHandler
  {
    private readonly HttpMessageInvoker _invoker = new(target);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      return _invoker.SendAsync(request, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing)
        _invoker.Dispose();

      base.Dispose(disposing);
    }
  }
}
