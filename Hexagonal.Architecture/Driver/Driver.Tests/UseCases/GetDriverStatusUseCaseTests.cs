using Driver.Core.Mapping;
using Driver.Core.Ports.Secondary;
using Driver.Core.Ports.Primary;
using Driver.Core.UseCases;
using DriverEntity = Driver.Core.Domain.Driver;
using NSubstitute;
using Xunit;

namespace Driver.Tests.UseCases;

public class GetDriverStatusUseCaseTests
{
  [Fact]
  public async Task GetDriverStatus_ShouldUseCacheOnSecondCall()
  {
    var repository = Substitute.For<IDriverRepository>();
    var cache = new FakeCacheService();
    var id = Guid.NewGuid();
    var driver = new DriverEntity(id);
    repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(driver);

    var useCase = new GetDriverStatusUseCase(repository, new DriverMapper(), cache);

    var first = await useCase.Handle(new GetDriverStatus { Id = id, CallerId = id }, CancellationToken.None);

    driver.UpdateAvailability(Driver.Core.Domain.DriverStatus.OnRide); // shouldn't affect cached result

    var second = await useCase.Handle(new GetDriverStatus { Id = id, CallerId = id }, CancellationToken.None);

    Assert.Equivalent(first, second);
    Assert.Equal(Driver.Core.Domain.DriverStatus.Available, second.Status);
    await repository.Received(1).GetByIdAsync(id, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetDriverStatus_ShouldRejectACallerReadingAnotherDriver()
  {
    var repository = Substitute.For<IDriverRepository>();
    var id = Guid.NewGuid();
    repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(new DriverEntity(id));

    var useCase = new GetDriverStatusUseCase(repository, new DriverMapper(), new FakeCacheService());

    await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
      useCase.Handle(new GetDriverStatus { Id = id, CallerId = Guid.NewGuid() }, CancellationToken.None));

    await repository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetDriverStatus_ShouldNotServeAForeignCallerFromAWarmCache()
  {
    // The driver's own read populates the cache first, so this fails if the ownership check sits
    // inside the cache factory rather than in front of it.
    var repository = Substitute.For<IDriverRepository>();
    var id = Guid.NewGuid();
    repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(new DriverEntity(id));

    var useCase = new GetDriverStatusUseCase(repository, new DriverMapper(), new FakeCacheService());
    await useCase.Handle(new GetDriverStatus { Id = id, CallerId = id }, CancellationToken.None);

    await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
      useCase.Handle(new GetDriverStatus { Id = id, CallerId = Guid.NewGuid() }, CancellationToken.None));
  }
}
