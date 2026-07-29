using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Hub.Tests")]
// NSubstitute (Castle DynamicProxy) needs this to mock IHubContext<VoyagerHub, IVoyagerShareClient> in tests, since VoyagerHub is internal.
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
