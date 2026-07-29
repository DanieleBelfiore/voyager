using Driver.Core.Ports.Primary;
using Driver.Core.Ports.Secondary;
using Driver.Core.UseCases;
using DriverEntity = Driver.Core.Domain.Driver;
using NetTopologySuite.Geometries;
using NSubstitute;
using Voyager.Contracts.Driver;
using Xunit;

namespace Driver.Tests.UseCases;

public class UpdateLocationUseCaseTests
{
  private readonly IDriverRepository _repository;
  private readonly ICacheService _cache;
  private readonly UpdateLocationUseCase _useCase;

  public UpdateLocationUseCaseTests()
  {
    _repository = Substitute.For<IDriverRepository>();
    _cache = Substitute.For<ICacheService>();
    _useCase = new UpdateLocationUseCase(_repository, _cache);
  }

  [Fact]
  public async Task Handle_UpdatesLocationAndInvalidatesCache_WhenDriverExists()
  {
    // Arrange
    var id = Guid.NewGuid();
    var driver = new DriverEntity(id);
    var location = new Point(1, 2);
    _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(driver);

    // Act
    await _useCase.Handle(new UpdateLocation { Id = id, Location = location }, CancellationToken.None);

    // Assert
    Assert.Equal(location, driver.LastLocation);
    await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    await _cache.Received(1).RemoveAsync($"driver:status:{id}");
  }

  [Fact]
  public async Task Handle_Throws_WhenDriverNotFound()
  {
    // Arrange
    var id = Guid.NewGuid();
    _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((DriverEntity?)null);
    var act = () => _useCase.Handle(new UpdateLocation { Id = id, Location = new Point(0, 0) }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("driver_not_found", ex.Message);
    await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>());
  }
}
