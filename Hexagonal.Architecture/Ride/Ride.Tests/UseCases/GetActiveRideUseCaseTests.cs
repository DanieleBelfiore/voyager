using NetTopologySuite.Geometries;
using Ride.Core.Mapping;
using Ride.Core.Ports.Secondary;
using Ride.Core.Ports.Primary;
using Ride.Core.UseCases;
using RideEntity = Ride.Core.Domain.Ride;
using NSubstitute;
using Xunit;

namespace Ride.Tests.UseCases;

public class GetActiveRideUseCaseTests
{
  [Fact]
  public async Task GetActiveRide_ShouldReturnMappedRide_WhenFound()
  {
    var repository = Substitute.For<IRideRepository>();
    var userId = Guid.NewGuid();
    var ride = new RideEntity(userId, Guid.NewGuid(), new Point(0, 0), new Point(1, 1));
    repository.GetActiveRideAsync(null, userId, Arg.Any<CancellationToken>()).Returns(ride);

    var useCase = new GetActiveRideUseCase(repository, new RideMapper());

    var result = await useCase.Handle(new GetActiveRide { UserId = userId }, CancellationToken.None);

    Assert.NotNull(result);
    Assert.Equal(ride.Id, result!.Id);
    Assert.Equal(userId, result.UserId);
  }

  [Fact]
  public async Task GetActiveRide_ShouldReturnNull_WhenNoneFound()
  {
    var repository = Substitute.For<IRideRepository>();
    repository.GetActiveRideAsync(Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns((RideEntity?)null);

    var useCase = new GetActiveRideUseCase(repository, new RideMapper());

    var result = await useCase.Handle(new GetActiveRide { UserId = Guid.NewGuid() }, CancellationToken.None);

    Assert.Null(result);
  }
}
