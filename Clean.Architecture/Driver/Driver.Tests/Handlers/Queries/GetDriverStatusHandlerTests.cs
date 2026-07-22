using Driver.Application.CQRS.Queries;
using Driver.Application.Mapping;
using Driver.Application.Ports;
using DriverEntity = Driver.Domain.Entities.Driver;
using FluentAssertions;
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
    var result = await _handler.Handle(new GetDriverStatus { Id = id }, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(id);
    result.Status.Should().Be(Driver.Domain.Enums.DriverStatus.Available);

    var cached = await _cache.GetAsync<Driver.Application.Dtos.DriverStatusResponse>($"driver:status:{id}");
    cached.Should().NotBeNull();
    cached.Id.Should().Be(id);
  }

  [Fact]
  public async Task GetDriverStatus_ShouldUseCacheOnSecondCall()
  {
    // Arrange
    var id = Guid.NewGuid();
    var driver = new DriverEntity(id);
    _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(driver);

    // Act
    var firstResult = await _handler.Handle(new GetDriverStatus { Id = id }, CancellationToken.None);

    driver.UpdateAvailability(Driver.Domain.Enums.DriverStatus.OnRide); // shouldn't affect cached result

    var secondResult = await _handler.Handle(new GetDriverStatus { Id = id }, CancellationToken.None);

    // Assert
    secondResult.Should().BeEquivalentTo(firstResult);
    secondResult.Status.Should().Be(Driver.Domain.Enums.DriverStatus.Available); // still the cached value
    await _repository.Received(1).GetByIdAsync(id, Arg.Any<CancellationToken>()); // repository hit only once
  }
}
