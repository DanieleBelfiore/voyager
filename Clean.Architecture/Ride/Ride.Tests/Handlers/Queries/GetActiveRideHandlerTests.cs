using NetTopologySuite.Geometries;
using Ride.Application.CQRS.Queries;
using Ride.Application.Mapping;
using Ride.Application.Ports;
using RideEntity = Ride.Domain.Entities.Ride;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ride.Tests.Handlers.Queries;

public class GetActiveRideHandlerTests
{
  [Fact]
  public async Task GetActiveRide_ShouldReturnMappedRide_WhenFound()
  {
    var repository = Substitute.For<IRideRepository>();
    var userId = Guid.NewGuid();
    var ride = new RideEntity(userId, Guid.NewGuid(), new Point(0, 0), new Point(1, 1));
    repository.GetActiveRideAsync(null, userId, Arg.Any<CancellationToken>()).Returns(ride);

    var handler = new GetActiveRideHandler(repository, new RideMapper());

    var result = await handler.Handle(new GetActiveRide { UserId = userId }, CancellationToken.None);

    result.Should().NotBeNull();
    result!.Id.Should().Be(ride.Id);
    result.UserId.Should().Be(userId);
  }

  [Fact]
  public async Task GetActiveRide_ShouldReturnNull_WhenNoneFound()
  {
    var repository = Substitute.For<IRideRepository>();
    repository.GetActiveRideAsync(Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns((RideEntity?)null);

    var handler = new GetActiveRideHandler(repository, new RideMapper());

    var result = await handler.Handle(new GetActiveRide { UserId = Guid.NewGuid() }, CancellationToken.None);

    result.Should().BeNull();
  }
}
