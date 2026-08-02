using Driver.Core.Domain;
using Driver.Core.Ports.Secondary;
using Driver.Core.UseCases;
using DriverEntity = Driver.Core.Domain.Driver;
using NSubstitute;
using Voyager.Contracts.Driver;
using Xunit;

namespace Driver.Tests.UseCases;

public class DriverAvailabilityUseCasesTests
{
  private readonly IDriverRepository _repository = Substitute.For<IDriverRepository>();

  [Fact]
  public async Task MarkDriverOnRideUseCase_SetsStatusToOnRide()
  {
    var driver = new DriverEntity(Guid.NewGuid());
    _repository.GetByIdAsync(driver.Id, Arg.Any<CancellationToken>()).Returns(driver);
    var useCase = new MarkDriverOnRideUseCase(_repository);

    await useCase.Handle(new MarkDriverOnRide { DriverId = driver.Id }, CancellationToken.None);

    Assert.Equal(DriverStatus.OnRide, driver.Status);
    await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task MarkDriverOnRideUseCase_Throws_WhenDriverNotFound()
  {
    _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DriverEntity?)null);
    var useCase = new MarkDriverOnRideUseCase(_repository);
    var act = () => useCase.Handle(new MarkDriverOnRide { DriverId = Guid.NewGuid() }, CancellationToken.None);

    var ex = await Assert.ThrowsAsync<KeyNotFoundException>(act);
    Assert.Equal("driver_not_found", ex.Message);
  }

  [Fact]
  public async Task MarkDriverAvailableUseCase_SetsStatusToAvailable()
  {
    var driver = new DriverEntity(Guid.NewGuid());
    driver.UpdateAvailability(DriverStatus.OnRide);
    _repository.GetByIdAsync(driver.Id, Arg.Any<CancellationToken>()).Returns(driver);
    var useCase = new MarkDriverAvailableUseCase(_repository);

    await useCase.Handle(new MarkDriverAvailable { DriverId = driver.Id }, CancellationToken.None);

    Assert.Equal(DriverStatus.Available, driver.Status);
    await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task MarkDriverAvailableUseCase_Throws_WhenDriverNotFound()
  {
    _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DriverEntity?)null);
    var useCase = new MarkDriverAvailableUseCase(_repository);
    var act = () => useCase.Handle(new MarkDriverAvailable { DriverId = Guid.NewGuid() }, CancellationToken.None);

    var ex = await Assert.ThrowsAsync<KeyNotFoundException>(act);
    Assert.Equal("driver_not_found", ex.Message);
  }
}
