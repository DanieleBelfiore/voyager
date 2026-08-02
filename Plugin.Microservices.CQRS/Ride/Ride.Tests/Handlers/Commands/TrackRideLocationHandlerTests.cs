using NetTopologySuite.Geometries;
using Ride.Core.CQRS.Commands;
using Ride.Core.Enums;
using Ride.Handlers.CQRS.Commands;
using Xunit;

namespace Ride.Tests.Handlers.Commands;

/// <summary>
/// LastLocation used to be written only at Start and Complete, which left
/// GET /rides/{id}/location reporting the pickup point for the entire trip.
/// </summary>
public class TrackRideLocationHandlerTests
{
  private readonly TestApplicationDbContext _context = TestBase.CreateTestDbContext();

  [Fact]
  public async Task Handle_RecordsTheNewPosition_WhileTheRideIsRunning()
  {
    // Arrange
    var ride = new Ride.Handlers.Models.Ride { Id = Guid.NewGuid(), Status = RideStatus.InProgress, LastLocation = new Point(0, 0) };
    _context.Rides.Add(ride);
    await _context.SaveChangesAsync();
    var position = new Point(9, 9);

    // Act
    await new TrackRideLocationHandler(_context).Handle(
      new TrackRideLocation { RideId = ride.Id, Location = position }, CancellationToken.None);

    // Assert
    Assert.Equal(position, ride.LastLocation);
    Assert.NotNull(ride.LastLocationGeoJSON);
  }

  [Fact]
  public async Task Handle_IgnoresAPositionForAFinishedRide()
  {
    // Arrange — a late report can legitimately land after Complete
    var dropoff = new Point(1, 1);
    var ride = new Ride.Handlers.Models.Ride { Id = Guid.NewGuid(), Status = RideStatus.Completed, LastLocation = dropoff };
    _context.Rides.Add(ride);
    await _context.SaveChangesAsync();

    // Act
    await new TrackRideLocationHandler(_context).Handle(
      new TrackRideLocation { RideId = ride.Id, Location = new Point(9, 9) }, CancellationToken.None);

    // Assert
    Assert.Equal(dropoff, ride.LastLocation);
  }

  [Fact]
  public async Task Handle_IsANoOp_WhenTheRideIsGone()
  {
    // Act
    var act = () => new TrackRideLocationHandler(_context).Handle(
      new TrackRideLocation { RideId = Guid.NewGuid(), Location = new Point(9, 9) }, CancellationToken.None);

    // Assert
    await act();
  }
}
