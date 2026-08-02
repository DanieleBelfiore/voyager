using System.Text.Json;
using Driver.Core.Dtos;
using Driver.Core.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NetTopologySuite.Geometries;
using NSubstitute;
using StackExchange.Redis;
using Common.Core.Cache;
using Xunit;

namespace Driver.Tests;

/// <summary>
/// The cache only ever holds DriverStatusResponse, and that DTO carries a NetTopologySuite
/// Point. Plain System.Text.Json cannot write one (Point.Z/M are NaN), so every write threw and
/// was swallowed: the cache silently never populated and every lookup hit SQL. These tests pin
/// the round-trip that has to keep working.
/// </summary>
public class RedisCacheServiceTests
{
  private readonly IDatabase _db = Substitute.For<IDatabase>();
  private readonly RedisCacheService _cache;
  private RedisValue _written = RedisValue.Null;

  public RedisCacheServiceTests()
  {
    var multiplexer = Substitute.For<IConnectionMultiplexer>();
    multiplexer.GetDatabase().Returns(_db);

    _db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<Expiration>(), Arg.Any<ValueCondition>(), Arg.Any<CommandFlags>())
      .Returns(call => { _written = call.ArgAt<RedisValue>(1); return Task.FromResult(true); });

    _db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
      .Returns(_ => Task.FromResult(_written));

    _cache = new RedisCacheService(multiplexer, NullLogger<RedisCacheService>.Instance);
  }

  private static DriverStatusResponse SomeDriver() => new()
  {
    Id = Guid.NewGuid(),
    Status = DriverStatus.OnRide,
    LastLocation = new Point(12.4963680, 41.9027835) { SRID = 4326 },
    LastUpdateDate = DateTime.UtcNow
  };

  [Fact]
  public async Task GetOrCreateAsync_ActuallyWritesTheValueToRedis()
  {
    // Arrange
    var driver = SomeDriver();

    // Act
    await _cache.GetOrCreateAsync("driver:status:x", () => Task.FromResult(driver), TimeSpan.FromMinutes(5));

    // Assert — before the GeoJson converter this never ran: Serialize threw first
    Assert.True(_written.HasValue);
  }

  [Fact]
  public async Task GetOrCreateAsync_ServesTheSecondCallFromCache_WithGeometryIntact()
  {
    // Arrange
    var driver = SomeDriver();
    var factoryCalls = 0;
    Task<DriverStatusResponse> Factory() { factoryCalls++; return Task.FromResult(driver); }

    // Act
    await _cache.GetOrCreateAsync("driver:status:x", Factory, TimeSpan.FromMinutes(5));
    var cached = await _cache.GetOrCreateAsync("driver:status:x", Factory, TimeSpan.FromMinutes(5));

    // Assert
    Assert.Equal(1, factoryCalls);
    Assert.Equal(driver.Id, cached.Id);
    Assert.Equal(driver.Status, cached.Status);
    Assert.Equal(driver.LastLocation.X, cached.LastLocation.X, 9);
    Assert.Equal(driver.LastLocation.Y, cached.LastLocation.Y, 9);
  }

  [Fact]
  public async Task GetOrCreateAsync_HandlesADriverWithNoLocationYet()
  {
    // Arrange
    var driver = SomeDriver();
    driver.LastLocation = null;

    // Act
    await _cache.GetOrCreateAsync("driver:status:x", () => Task.FromResult(driver), TimeSpan.FromMinutes(5));
    var cached = await _cache.GetOrCreateAsync("driver:status:x", () => Task.FromResult(driver), TimeSpan.FromMinutes(5));

    // Assert
    Assert.Null(cached.LastLocation);
  }

  [Fact]
  public async Task SetAsync_SurfacesASerializationFailure_InsteadOfSwallowingIt()
  {
    // Arrange — a type System.Text.Json genuinely cannot handle is a caller bug, not a Redis
    // outage, and hiding it is what kept the Point failure invisible for so long.
    var unserializable = new NotSerializable();

    // Act
    var act = () => _cache.GetOrCreateAsync("k", () => Task.FromResult(unserializable), TimeSpan.FromMinutes(5));

    // Assert
    await Assert.ThrowsAnyAsync<JsonException>(act);
  }

  private class NotSerializable
  {
    public NotSerializable Self => this;
  }
}
