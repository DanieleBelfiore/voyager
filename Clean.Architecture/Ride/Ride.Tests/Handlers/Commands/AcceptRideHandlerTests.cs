using NetTopologySuite.Geometries;
using Ride.Application.CQRS.Commands;
using Ride.Application.Ports;
using RideEntity = Ride.Domain.Entities.Ride;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ride.Tests.Handlers.Commands;

public class AcceptRideHandlerTests
{
  private static readonly Point SomePoint = new(0, 0);

  [Fact]
  public async Task AcceptRide_ShouldAssignDriverAndPersist()
  {
    var repository = Substitute.For<IRideRepository>();
    var events = Substitute.For<IRideEventPublisher>();
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), SomePoint, SomePoint);
    repository.GetByIdAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);

    var handler = new AcceptRideHandler(repository, events);
    var driverId = Guid.NewGuid();

    await handler.Handle(new AcceptRide { RideId = ride.Id, DriverId = driverId }, CancellationToken.None);

    ride.DriverId.Should().Be(driverId);
    ride.Status.Should().Be(Ride.Domain.Enums.RideStatus.DriverAssigned);
    await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    await events.Received(1).RideAcceptedAsync(ride.Id, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task AcceptRide_ShouldThrow_WhenRideNotFound()
  {
    var repository = Substitute.For<IRideRepository>();
    var events = Substitute.For<IRideEventPublisher>();
    repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((RideEntity?)null);

    var handler = new AcceptRideHandler(repository, events);

    var act = () => handler.Handle(new AcceptRide { RideId = Guid.NewGuid(), DriverId = Guid.NewGuid() }, CancellationToken.None);

    await act.Should().ThrowAsync<Exception>().WithMessage("no_ride_found");
  }
}
