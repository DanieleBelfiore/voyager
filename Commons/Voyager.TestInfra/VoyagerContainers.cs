using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using StackExchange.Redis;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit;

namespace Voyager.TestInfra;

/// <summary>
/// The infrastructure every variant's integration suite runs against: one SQL Server, one
/// RabbitMQ, one Redis. Shared for a whole test assembly through an <c>ICollectionFixture</c> —
/// a per-class fixture would start its own containers for every test class, which stops scaling
/// after a couple of them.
/// </summary>
/// <remarks>
/// One SQL Server hosting three catalogues rather than three servers: the variants keep a
/// database per bounded context (<c>identity</c>, <c>driver</c>, <c>ride</c>), and a catalogue
/// boundary is what their connection strings actually express.
/// </remarks>
public class VoyagerContainers : IAsyncLifetime
{
  public const string RabbitMqUserName = "voyager";
  public const string RabbitMqPassword = "voyager";

  private const string SaPassword = "Strong!Passw0rd";

  private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest")
    .WithEnvironment("ACCEPT_EULA", "Y")
    .WithEnvironment("MSSQL_SA_PASSWORD", SaPassword)
    .WithEnvironment("SQLCMDPASSWORD", SaPassword)
    .WithPassword(SaPassword)
    .Build();

  private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:4.3-management")
    .WithUsername(RabbitMqUserName)
    .WithPassword(RabbitMqPassword)
    .Build();

  private readonly RedisContainer _redis = new RedisBuilder("redis:7").Build();

  public string RabbitMqHostName => _rabbitMq.Hostname;
  public int RabbitMqPort => _rabbitMq.GetMappedPublicPort(5672);
  public string RedisConnectionString => _redis.GetConnectionString();

  public Task InitializeAsync()
  {
    return Task.WhenAll(_sql.StartAsync(), _rabbitMq.StartAsync(), _redis.StartAsync());
  }

  public Task DisposeAsync()
  {
    return Task.WhenAll(_sql.StopAsync(), _rabbitMq.StopAsync(), _redis.StopAsync());
  }

  public string SqlConnectionString(string catalog)
  {
    return new SqlConnectionStringBuilder(_sql.GetConnectionString()) { InitialCatalog = catalog }.ConnectionString;
  }

  /// <summary>
  /// The raw payload behind a cache key, or <c>null</c> if nothing was written.
  /// </summary>
  /// <remarks>
  /// Read straight out of Redis rather than through <c>ICacheService</c> on purpose. The failure
  /// this guards against is a write that throws and gets swallowed: going back through the cache
  /// abstraction would report a miss either way, so only the store itself can tell "written" from
  /// "silently dropped".
  /// </remarks>
  public async Task<string> ReadCacheEntry(string key)
  {
    using var redis = await ConnectionMultiplexer.ConnectAsync(RedisConnectionString);

    return await redis.GetDatabase().StringGetAsync(key);
  }

  /// <summary>
  /// Publishes <see cref="Configuration"/> as environment variables, which is the only injection
  /// point early enough to be seen by every consumer.
  /// </summary>
  /// <remarks>
  /// A configuration source added through <c>ConfigureAppConfiguration</c> is merged when the
  /// host is built — after <c>Program.cs</c> has already run its <c>builder.Services.Add…</c>
  /// calls. That is invisible for anything reading configuration inside a factory lambda (the
  /// <c>DbContext</c> registrations resolve their connection string per instance, so they pick the
  /// override up), and fatal for anything reading it eagerly at registration: <c>AddRedisCache</c>
  /// binds <c>Redis:ConnectionString</c> right there and hands the value to a singleton
  /// <c>ConnectionMultiplexer</c>, so the host stayed pointed at the compose hostname and every
  /// cache write silently went nowhere. Environment variables are in the default configuration
  /// from <c>CreateBuilder</c> onward, so both kinds of consumer see them.
  /// </remarks>
  public void ExportToEnvironment()
  {
    foreach (var entry in Configuration())
      Environment.SetEnvironmentVariable(entry.Key.Replace(":", "__"), entry.Value);
  }

  /// <summary>
  /// The configuration keys every variant reads, pointed at these containers. Keys a given
  /// variant does not use are simply ignored.
  /// </summary>
  public Dictionary<string, string> Configuration()
  {
    var configuration = new Dictionary<string, string>
    {
      ["ConnectionStrings:IdentityContext"] = SqlConnectionString("identity"),
      ["ConnectionStrings:DriverContext"] = SqlConnectionString("driver"),
      ["ConnectionStrings:RideContext"] = SqlConnectionString("ride"),
      ["RabbitMQ:HostName"] = RabbitMqHostName,
      ["RabbitMQ:Port"] = RabbitMqPort.ToString(),
      ["RabbitMQ:UserName"] = RabbitMqUserName,
      ["RabbitMQ:Password"] = RabbitMqPassword,
      ["RabbitMQ:VirtualHost"] = "/",
      ["Redis:ConnectionString"] = RedisConnectionString,
      // Signing certificates are refused outside Development unless this is set, and the test
      // host runs as Production so that the rest of the pipeline matches a deployment.
      ["Identity:UseDevelopmentCertificates"] = "true"
    };

    // Rate limits are per host, and one host serves the whole suite. The shipped numbers are
    // sized for a human (identity_register allows five registrations an hour), so a suite that
    // registers a fresh user per test hits the queue and every later request blocks until the
    // client gives up. Raised, not removed: the limiter middleware stays in the pipeline, so
    // partitioning and policy resolution are still exercised on every request.
    foreach (var policy in new[]
             {
               "identity_token", "identity_register",
               "driver_api", "driver_registration", "driver_status_update", "driver_location_update",
               "driver_status", "driver_search",
               "ride_api", "ride_request", "ride_cancellation", "ride_location"
             })
    {
      configuration[$"RateLimiting:EndpointLimits:{policy}:PermitLimit"] = "100000";
      configuration[$"RateLimiting:EndpointLimits:{policy}:Window"] = "60";
      configuration[$"RateLimiting:EndpointLimits:{policy}:Policy"] = "fixed";
    }

    configuration["RateLimiting:PermitLimit"] = "100000";

    return configuration;
  }
}
