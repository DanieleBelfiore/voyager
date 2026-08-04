using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Hub.Tests")]
// NSubstitute (Castle DynamicProxy) needs this to mock IHubContext<VoyagerHub, IVoyagerShareClient> in tests, since VoyagerHub is internal.
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
// SignalR emits a proxy implementing IVoyagerShareClient at runtime, into an assembly of its own.
// Without this, resolving IHubContext<VoyagerHub, IVoyagerShareClient> throws TypeLoadException
// ("attempting to implement an inaccessible interface") the first time anything publishes to a
// client — so every endpoint that raises a real-time event answers 500. Unit tests never see it:
// they mock IHubContext rather than letting SignalR build the proxy.
[assembly: InternalsVisibleTo("Microsoft.AspNetCore.SignalR.TypedClientBuilder")]
