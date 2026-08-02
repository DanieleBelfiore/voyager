using Driver.Core.Ports.Primary;
using Driver.Core.Ports.Secondary;
using Driver.Core.UseCases;
using DriverEntity = Driver.Core.Domain.Driver;
using NSubstitute;
using Xunit;

namespace Driver.Tests.UseCases;

public class UpdateAvailabilityUseCaseTests
{
  private readonly IDriverRepository _repository;
  private readonly UpdateAvailabilityUseCase _useCase;

  public UpdateAvailabilityUseCaseTests()
  {
    _repository = Substitute.For<IDriverRepository>();
    _useCase = new UpdateAvailabilityUseCase(_repository);
  }

  [Fact]
  public async Task Handle_UpdatesStatus_WhenDriverExists()
  {
    // Arrange
    var id = Guid.NewGuid();
    var driver = new DriverEntity(id);
    _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(driver);

    // Act
    await _useCase.Handle(new UpdateAvailability { Id = id, Status = Driver.Core.Domain.DriverStatus.OnRide }, CancellationToken.None);

    // Assert
    Assert.Equal(Driver.Core.Domain.DriverStatus.OnRide, driver.Status);
    await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_Throws_WhenDriverNotFound()
  {
    // Arrange
    var id = Guid.NewGuid();
    _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((DriverEntity?)null);
    var act = () => _useCase.Handle(new UpdateAvailability { Id = id, Status = Driver.Core.Domain.DriverStatus.Available }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<KeyNotFoundException>(act);
    Assert.Equal("driver_not_found", ex.Message);
    await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }
}
