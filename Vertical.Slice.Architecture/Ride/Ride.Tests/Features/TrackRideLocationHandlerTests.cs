using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Ride.Api.Features.TrackRideLocation;
using Ride.Api.Persistence;
using SharedTrackRideLocation = Voyager.Contracts.Ride.TrackRideLocation;
using Xunit;
using RideEntity = Ride.Api.Entities.Ride;

namespace Ride.Tests.Features;

/// <summary>
/// LastLocation used to be written only at Start and Complete, which left
/// GET /rides/{id}/location reporting the pickup point for the entire trip.
/// </summary>
public class TrackRideLocationHandlerTests
{
  private static RideDbContext NewContext() =>
    new(new DbContextOptionsBuilder<RideDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

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
    await using var db = NewContext();
    var ride = StartedRide();
    db.Rides.Add(ride);
    await db.SaveChangesAsync();
    var position = new Point(9, 9);

    // Act
    await new TrackRideLocationHandler(db).Handle(
      new SharedTrackRideLocation { RideId = ride.Id, Location = position }, CancellationToken.None);

    // Assert
    Assert.Equal(position, ride.LastLocation);
  }

  [Fact]
  public async Task Handle_IgnoresAPositionForAFinishedRide()
  {
    // Arrange — a late report can legitimately land after Complete
    await using var db = NewContext();
    var ride = StartedRide();
    ride.Complete(new Point(1, 1), 12.34);
    db.Rides.Add(ride);
    await db.SaveChangesAsync();
    var dropoff = ride.LastLocation;

    // Act
    await new TrackRideLocationHandler(db).Handle(
      new SharedTrackRideLocation { RideId = ride.Id, Location = new Point(9, 9) }, CancellationToken.None);

    // Assert
    Assert.Equal(dropoff, ride.LastLocation);
  }

  [Fact]
  public async Task Handle_IsANoOp_WhenTheRideIsGone()
  {
    // Arrange
    await using var db = NewContext();

    // Act
    var act = () => new TrackRideLocationHandler(db).Handle(
      new SharedTrackRideLocation { RideId = Guid.NewGuid(), Location = new Point(9, 9) }, CancellationToken.None);

    // Assert
    await act();
  }
}
