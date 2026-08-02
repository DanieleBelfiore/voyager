using NetTopologySuite.Geometries;
using NSubstitute;
using Ride.Application.CQRS.Commands;
using Ride.Application.Ports;
using RideEntity = Ride.Domain.Entities.Ride;
using SharedTrackRideLocation = Voyager.Contracts.Ride.TrackRideLocation;
using Xunit;

namespace Ride.Tests.Handlers.Commands;

/// <summary>
/// LastLocation used to be written only at Start and Complete, which left
/// GET /rides/{id}/location reporting the pickup point for the entire trip.
/// </summary>
public class TrackRideLocationHandlerTests
{
  private readonly IRideRepository _repository = Substitute.For<IRideRepository>();
  private readonly TrackRideLocationHandler _handler;

  public TrackRideLocationHandlerTests()
  {
    _handler = new TrackRideLocationHandler(_repository);
  }

  private static RideEntity StartedRide()
  {
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), new Point(0, 0), new Point(1, 1));
    ride.Accept(ride.DriverId);
    ride.Start(new Point(0, 0));

    return ride;
  }

  [Fact]
  public async Task Handle_RecordsTheNewPosition_WhileTheRideIsRunning()
  {
    // Arrange
    var ride = StartedRide();
    _repository.GetByIdAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);
    var position = new Point(9, 9);

    // Act
    await _handler.Handle(new SharedTrackRideLocation { RideId = ride.Id, Location = position }, CancellationToken.None);

    // Assert
    Assert.Equal(position, ride.LastLocation);
    await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_IgnoresAPositionForAFinishedRide()
  {
    // Arrange — a late report can legitimately land after Complete
    var ride = StartedRide();
    ride.Complete(new Point(1, 1), 12.34);
    var dropoff = ride.LastLocation;
    _repository.GetByIdAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);

    // Act
    await _handler.Handle(new SharedTrackRideLocation { RideId = ride.Id, Location = new Point(9, 9) }, CancellationToken.None);

    // Assert
    Assert.Equal(dropoff, ride.LastLocation);
  }

  [Fact]
  public async Task Handle_IsANoOp_WhenTheRideIsGone()
  {
    // Arrange
    _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((RideEntity?)null);

    // Act
    await _handler.Handle(new SharedTrackRideLocation { RideId = Guid.NewGuid(), Location = new Point(9, 9) }, CancellationToken.None);

    // Assert
    await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }
}
