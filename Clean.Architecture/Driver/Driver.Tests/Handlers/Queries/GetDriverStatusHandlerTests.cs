using Driver.Application.CQRS.Queries;
using Driver.Application.Mapping;
using Driver.Application.Ports;
using DriverEntity = Driver.Domain.Entities.Driver;
using NSubstitute;
using Xunit;

namespace Driver.Tests.Handlers.Queries;

public class GetDriverStatusHandlerTests
{
  private readonly IDriverRepository _repository;
  private readonly ICacheService _cache;
  private readonly GetDriverStatusHandler _handler;

  public GetDriverStatusHandlerTests()
  {
    _repository = Substitute.For<IDriverRepository>();
    _cache = new FakeCacheService();
    _handler = new GetDriverStatusHandler(_repository, new DriverMapper(), _cache);
  }

  [Fact]
  public async Task GetDriverStatusTestFact()
  {
    // Arrange
    var id = Guid.NewGuid();
    _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(new DriverEntity(id));

    // Act
    var result = await _handler.Handle(new GetDriverStatus { Id = id, CallerId = id }, CancellationToken.None);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(id, result.Id);
    Assert.Equal(Driver.Domain.Enums.DriverStatus.Available, result.Status);

    var cached = await _cache.GetAsync<Driver.Application.Dtos.DriverStatusResponse>($"driver:status:{id}");
    Assert.NotNull(cached);
    Assert.Equal(id, cached.Id);
  }

  [Fact]
  public async Task GetDriverStatus_ShouldUseCacheOnSecondCall()
  {
    // Arrange
    var id = Guid.NewGuid();
    var driver = new DriverEntity(id);
    _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(driver);

    // Act
    var firstResult = await _handler.Handle(new GetDriverStatus { Id = id, CallerId = id }, CancellationToken.None);

    driver.UpdateAvailability(Driver.Domain.Enums.DriverStatus.OnRide); // shouldn't affect cached result

    var secondResult = await _handler.Handle(new GetDriverStatus { Id = id, CallerId = id }, CancellationToken.None);

    // Assert
    Assert.Equivalent(firstResult, secondResult);
    Assert.Equal(Driver.Domain.Enums.DriverStatus.Available, secondResult.Status); // still the cached value
    await _repository.Received(1).GetByIdAsync(id, Arg.Any<CancellationToken>()); // repository hit only once
  }

  [Fact]
  public async Task GetDriverStatus_ShouldRejectACallerReadingAnotherDriver()
  {
    // Arrange
    var driverId = Guid.NewGuid();
    _repository.GetByIdAsync(driverId, Arg.Any<CancellationToken>()).Returns(new DriverEntity(driverId));

    // Act + Assert
    await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
      _handler.Handle(new GetDriverStatus { Id = driverId, CallerId = Guid.NewGuid() }, CancellationToken.None));

    await _repository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetDriverStatus_ShouldNotServeAForeignCallerFromAWarmCache()
  {
    // Arrange — the driver's own read populates the cache first, so this fails if the ownership
    // check sits inside the cache factory rather than in front of it.
    var driverId = Guid.NewGuid();
    _repository.GetByIdAsync(driverId, Arg.Any<CancellationToken>()).Returns(new DriverEntity(driverId));
    await _handler.Handle(new GetDriverStatus { Id = driverId, CallerId = driverId }, CancellationToken.None);

    // Act + Assert
    await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
      _handler.Handle(new GetDriverStatus { Id = driverId, CallerId = Guid.NewGuid() }, CancellationToken.None));
  }
}
