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
    ride.Accept(ride.DriverId);
    repository.GetByIdAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);

    var useCase = new StartRideUseCase(repository);
    var startLocation = new Point(2, 2);

    await useCase.Handle(new StartRide { Id = ride.Id, Location = startLocation, CallerId = ride.DriverId }, CancellationToken.None);

    Assert.Equal(Ride.Core.Domain.RideStatus.InProgress, ride.Status);
    // Where the driver reports starting from is recorded, but it does not become the pickup:
    // PickupLocation is what the rider agreed to and what the fare is measured against, so
    // letting Start move it let a driver stretch the priced segment before the trip even began.
    Assert.Equal(new Point(0, 0), ride.PickupLocation);
    Assert.NotNull(ride.StartAt);
    await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }
}
