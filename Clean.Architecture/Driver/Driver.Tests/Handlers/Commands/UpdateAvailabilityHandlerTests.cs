using Driver.Application.CQRS.Commands;
using Driver.Application.Ports;
using DriverEntity = Driver.Domain.Entities.Driver;
using Driver.Domain.Enums;
using NSubstitute;
using Xunit;

namespace Driver.Tests.Handlers.Commands;

public class UpdateAvailabilityHandlerTests
{
  private readonly IDriverRepository _repository;
  private readonly UpdateAvailabilityHandler _handler;

  public UpdateAvailabilityHandlerTests()
  {
    _repository = Substitute.For<IDriverRepository>();
    _handler = new UpdateAvailabilityHandler(_repository);
  }

  [Fact]
  public async Task Handle_UpdatesStatus_WhenDriverExists()
  {
    // Arrange
    var id = Guid.NewGuid();
    var driver = new DriverEntity(id);
    _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(driver);

    // Act
    await _handler.Handle(new UpdateAvailability { Id = id, Status = DriverStatus.OnRide }, CancellationToken.None);

    // Assert
    Assert.Equal(DriverStatus.OnRide, driver.Status);
    await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_Throws_WhenDriverNotFound()
  {
    // Arrange
    var id = Guid.NewGuid();
    _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((DriverEntity?)null);
    var act = () => _handler.Handle(new UpdateAvailability { Id = id, Status = DriverStatus.Available }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<KeyNotFoundException>(act);
    Assert.Equal("driver_not_found", ex.Message);
    await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }
}
