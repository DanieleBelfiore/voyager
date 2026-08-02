using NetTopologySuite.Geometries;
using Ride.Core.Ports.Secondary;
using Ride.Core.Ports.Primary;
using Ride.Core.UseCases;
using RideEntity = Ride.Core.Domain.Ride;
using NSubstitute;
using Xunit;

namespace Ride.Tests.UseCases;

public class AcceptRideUseCaseTests
{
  private static readonly Point SomePoint = new(0, 0);

  [Fact]
  public async Task AcceptRide_ShouldAssignDriverAndPersist()
  {
    // The accepting driver must be the one the rider assigned at RequestRide time.
    var repository = Substitute.For<IRideRepository>();
    var events = Substitute.For<IRideEventPublisher>();
    var availability = Substitute.For<IDriverAvailabilityNotifier>();
    var driverId = Guid.NewGuid();
    var ride = new RideEntity(Guid.NewGuid(), driverId, SomePoint, SomePoint);
    repository.GetByIdAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);

    var useCase = new AcceptRideUseCase(repository, events, availability);

    await useCase.Handle(new AcceptRide { RideId = ride.Id, DriverId = driverId }, CancellationToken.None);

    Assert.Equal(driverId, ride.DriverId);
    Assert.Equal(Ride.Core.Domain.RideStatus.DriverAssigned, ride.Status);
    await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    await events.Received(1).RideAcceptedAsync(ride.Id, Arg.Any<CancellationToken>());
    await availability.Received(1).MarkOnRideAsync(driverId, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task AcceptRide_ShouldThrow_WhenCallerIsNotAssignedDriver()
  {
    // A different driver must not be able to hijack a ride assigned to someone else.
    var repository = Substitute.For<IRideRepository>();
    var events = Substitute.For<IRideEventPublisher>();
    var availability = Substitute.For<IDriverAvailabilityNotifier>();
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), SomePoint, SomePoint);
    repository.GetByIdAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);

    var useCase = new AcceptRideUseCase(repository, events, availability);

    var act = () => useCase.Handle(new AcceptRide { RideId = ride.Id, DriverId = Guid.NewGuid() }, CancellationToken.None);

    var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    Assert.Equal("not_ride_participant", ex.Message);
  }
}
