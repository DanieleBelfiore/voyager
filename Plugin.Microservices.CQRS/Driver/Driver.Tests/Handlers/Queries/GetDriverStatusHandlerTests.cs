using Common.Core.Cache;
using Driver.Core.CQRS.Queries;
using Driver.Core.Dtos;
using Driver.Core.Enums;
using Driver.Handlers;
using Driver.Handlers.CQRS.Queries;
using Hikyaku;
using NSubstitute;
using Xunit;

namespace Driver.Tests.Handlers.Queries;

public class GetDriverStatusHandlerTests
{
  private readonly IHikyaku _mediator;
  private readonly TestApplicationDbContext _context;
  private readonly ICacheService _cache;

  public GetDriverStatusHandlerTests()
  {
    var (context, cache) = TestBase.CreateTestServices();
    _context = context;
    _cache = cache;

    var mediatorMock = Substitute.For<IHikyaku>();

    _mediator = mediatorMock;

    var mapper = new DriverMapper();

    mediatorMock.Send(Arg.Any<GetDriverStatus>(), Arg.Any<CancellationToken>())
      .Returns(c => new GetDriverStatusHandler(_context, mapper, _cache)
        .Handle(c.Arg<GetDriverStatus>(), c.Arg<CancellationToken>()));
  }

  [Fact]
  public async Task GetDriverStatusTestFact()
  {
    // Arrange
    var id = Guid.NewGuid();

    _context.Drivers.Add(new Driver.Handlers.Models.Driver { Id = id });
    await _context.SaveChangesAsync();

    // Act
    var result = await _mediator.Send(new GetDriverStatus { Id = id, CallerId = id });

    // Assert
    Assert.NotNull(result);
    Assert.Equal(id, result.Id);
    Assert.Equal(DriverStatus.Available, result.Status);

    // Verify cache
    var cachedResult = await _cache.GetAsync<DriverStatusResponse>($"driver:status:{id}");
    Assert.NotNull(cachedResult);
    Assert.Equal(id, cachedResult.Id);
  }

  [Fact]
  public async Task GetDriverStatus_ShouldUseCacheOnSecondCall()
  {
    // Arrange
    var id = Guid.NewGuid();

    _context.Drivers.Add(new Driver.Handlers.Models.Driver { Id = id });
    await _context.SaveChangesAsync();

    // Act
    var firstResult = await _mediator.Send(new GetDriverStatus { Id = id, CallerId = id });

    // Modify DB (shouldn't affect cached result)
    var driver = await _context.Drivers.FindAsync(id);
    driver!.Status = DriverStatus.OnRide;
    await _context.SaveChangesAsync();

    var secondResult = await _mediator.Send(new GetDriverStatus { Id = id, CallerId = id });

    // Assert
    Assert.Equivalent(firstResult, secondResult);
    Assert.Equal(DriverStatus.Available, secondResult.Status); // Still has cached value
  }

  [Fact]
  public async Task GetDriverStatus_ShouldRejectACallerReadingAnotherDriver()
  {
    // Arrange
    var id = Guid.NewGuid();

    _context.Drivers.Add(new Driver.Handlers.Models.Driver { Id = id });
    await _context.SaveChangesAsync();

    // Act + Assert
    await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
      _mediator.Send(new GetDriverStatus { Id = id, CallerId = Guid.NewGuid() }));
  }

  [Fact]
  public async Task GetDriverStatus_ShouldNotServeAForeignCallerFromAWarmCache()
  {
    // Arrange — the driver's own read populates the cache first, so this fails if the ownership
    // check sits inside the cache factory rather than in front of it.
    var id = Guid.NewGuid();

    _context.Drivers.Add(new Driver.Handlers.Models.Driver { Id = id });
    await _context.SaveChangesAsync();

    await _mediator.Send(new GetDriverStatus { Id = id, CallerId = id });

    // Act + Assert
    await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
      _mediator.Send(new GetDriverStatus { Id = id, CallerId = Guid.NewGuid() }));
  }
}
