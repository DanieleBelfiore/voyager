using NetTopologySuite.Geometries;
using Ride.Core.Ports.Secondary;
using Ride.Core.Ports.Primary;
using Ride.Core.UseCases;
using RideEntity = Ride.Core.Domain.Ride;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ride.Tests.UseCases;

public class AcceptRideUseCaseTests
{
  private static readonly Point SomePoint = new(0, 0);

  [Fact]
  public async Task AcceptRide_ShouldAssignDriverAndPersist()
  {
    var repository = Substitute.For<IRideRepository>();
    var events = Substitute.For<IRideEventPublisher>();
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), SomePoint, SomePoint);
    repository.GetByIdAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);

    var useCase = new AcceptRideUseCase(repository, events);
    var driverId = Guid.NewGuid();

    await useCase.Handle(new AcceptRide { RideId = ride.Id, DriverId = driverId }, CancellationToken.None);

    ride.DriverId.Should().Be(driverId);
    ride.Status.Should().Be(Ride.Core.Domain.RideStatus.DriverAssigned);
    await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    await events.Received(1).RideAcceptedAsync(ride.Id, Arg.Any<CancellationToken>());
  }
}
