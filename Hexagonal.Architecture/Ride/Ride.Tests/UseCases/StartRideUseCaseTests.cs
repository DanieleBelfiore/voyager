using NetTopologySuite.Geometries;
using Ride.Core.Ports.Secondary;
using Ride.Core.Ports.Primary;
using Ride.Core.UseCases;
using RideEntity = Ride.Core.Domain.Ride;
using NSubstitute;
using Xunit;

namespace Ride.Tests.UseCases;

public class StartRideUseCaseTests
{
  [Fact]
  public async Task StartRide_ShouldSetInProgressAndLocation()
  {
    var repository = Substitute.For<IRideRepository>();
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), new Point(0, 0), new Point(1, 1));
    repository.GetByIdAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);

    var useCase = new StartRideUseCase(repository);
    var startLocation = new Point(2, 2);

    await useCase.Handle(new StartRide { Id = ride.Id, Location = startLocation }, CancellationToken.None);

    Assert.Equal(Ride.Core.Domain.RideStatus.InProgress, ride.Status);
    Assert.Equal(startLocation, ride.PickupLocation);
    Assert.NotNull(ride.StartAt);
    await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }
}
