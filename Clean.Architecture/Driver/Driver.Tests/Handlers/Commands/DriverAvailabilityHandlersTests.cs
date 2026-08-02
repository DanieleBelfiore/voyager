using Driver.Application.CQRS.Commands;
using Driver.Application.Ports;
using Driver.Domain.Enums;
using DriverEntity = Driver.Domain.Entities.Driver;
using NSubstitute;
using Voyager.Contracts.Driver;
using Xunit;

namespace Driver.Tests.Handlers.Commands;

public class DriverAvailabilityHandlersTests
{
  private readonly IDriverRepository _repository = Substitute.For<IDriverRepository>();

  [Fact]
  public async Task MarkDriverOnRideHandler_SetsStatusToOnRide()
  {
    // Arrange
    var driver = new DriverEntity(Guid.NewGuid());
    _repository.GetByIdAsync(driver.Id, Arg.Any<CancellationToken>()).Returns(driver);
    var handler = new MarkDriverOnRideHandler(_repository);

    // Act
    await handler.Handle(new MarkDriverOnRide { DriverId = driver.Id }, CancellationToken.None);

    // Assert
    Assert.Equal(DriverStatus.OnRide, driver.Status);
    await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task MarkDriverOnRideHandler_Throws_WhenDriverNotFound()
  {
    // Arrange
    _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DriverEntity?)null);
    var handler = new MarkDriverOnRideHandler(_repository);
    var act = () => handler.Handle(new MarkDriverOnRide { DriverId = Guid.NewGuid() }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<KeyNotFoundException>(act);
    Assert.Equal("driver_not_found", ex.Message);
  }

  [Fact]
  public async Task MarkDriverAvailableHandler_SetsStatusToAvailable()
  {
    // Arrange
    var driver = new DriverEntity(Guid.NewGuid());
    driver.UpdateAvailability(DriverStatus.OnRide);
    _repository.GetByIdAsync(driver.Id, Arg.Any<CancellationToken>()).Returns(driver);
    var handler = new MarkDriverAvailableHandler(_repository);

    // Act
    await handler.Handle(new MarkDriverAvailable { DriverId = driver.Id }, CancellationToken.None);

    // Assert
    Assert.Equal(DriverStatus.Available, driver.Status);
    await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task MarkDriverAvailableHandler_Throws_WhenDriverNotFound()
  {
    // Arrange
    _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DriverEntity?)null);
    var handler = new MarkDriverAvailableHandler(_repository);
    var act = () => handler.Handle(new MarkDriverAvailable { DriverId = Guid.NewGuid() }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<KeyNotFoundException>(act);
    Assert.Equal("driver_not_found", ex.Message);
  }
}
