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

    var first = await useCase.Handle(new GetDriverStatus { Id = id }, CancellationToken.None);

    driver.UpdateAvailability(Driver.Core.Domain.DriverStatus.OnRide); // shouldn't affect cached result

    var second = await useCase.Handle(new GetDriverStatus { Id = id }, CancellationToken.None);

    Assert.Equivalent(first, second);
    Assert.Equal(Driver.Core.Domain.DriverStatus.Available, second.Status);
    await repository.Received(1).GetByIdAsync(id, Arg.Any<CancellationToken>());
  }
}
