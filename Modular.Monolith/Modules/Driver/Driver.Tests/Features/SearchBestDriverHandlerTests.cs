using Driver.Module.Entities;
using DriverEntity = Driver.Module.Entities.Driver;
using Driver.Module.Features.SearchBestDriver;
using Driver.Module.Persistence;
using Hikyaku;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using NSubstitute;
using Voyager.Contracts.Identity;
using Xunit;

namespace Driver.Tests.Features;

/// <summary>
/// EF Core's InMemory provider evaluates Point.Distance() as NTS's own planar/Cartesian
/// distance on raw coordinate values, not SQL Server's geodetic STDistance the handler actually
/// runs against in production — so distances here are in "coordinate units", not meters, and
/// these tests validate the handler's filtering/scoring/wiring logic only.
/// </summary>
public class SearchBestDriverHandlerTests
{
  private readonly IHikyaku _mediator = Substitute.For<IHikyaku>();
  private readonly IOptions<MatchingWeights> _weights = Options.Create(new MatchingWeights
  {
    DistanceWeight = 0.5,
    RatingWeight = 0.5,
    UserMinRating = 0.0,
    UserMaxRating = 5.0
  });

  private static DriverDbContext NewContext()
  {
    var options = new DbContextOptionsBuilder<DriverDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    return new DriverDbContext(options);
  }

  private static DriverEntity DriverAt(Point location)
  {
    var driver = new DriverEntity(Guid.NewGuid());
    driver.UpdateLocation(location);
    return driver;
  }

  [Fact]
  public async Task Handle_ReturnsEmpty_WhenNoDriverWithinThreshold()
  {
    // Arrange: (100, 100) is far outside a threshold of 5 coordinate units under either
    // distance semantics.
    await using var db = NewContext();
    var farDriver = DriverAt(new Point(100, 100));
    db.Drivers.Add(farDriver);
    await db.SaveChangesAsync();
    var handler = new SearchBestDriverHandler(db, _weights, _mediator);

    // Act
    var result = await handler.Handle(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(0, 0), DistanceThresholdInMeters = 5 }, CancellationToken.None);

    // Assert
    Assert.Empty(result);
    await _mediator.DidNotReceive().Send(Arg.Any<GetUsersRatings>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_ReturnsDriver_WhenWithinThreshold()
  {
    // Arrange
    await using var db = NewContext();
    var driver = DriverAt(new Point(0, 0.01));
    db.Drivers.Add(driver);
    await db.SaveChangesAsync();
    _mediator.Send(Arg.Any<GetUsersRatings>(), Arg.Any<CancellationToken>())
      .Returns(new Dictionary<Guid, double> { [driver.Id] = 4.0 });
    var handler = new SearchBestDriverHandler(db, _weights, _mediator);

    // Act
    var result = await handler.Handle(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(0, 0), DistanceThresholdInMeters = 5000 }, CancellationToken.None);

    // Assert
    var response = Assert.Single(result);
    Assert.Equal(driver.Id, response.DriverId);
    Assert.True(response.Distance >= 0);
  }

  [Fact]
  public async Task Handle_OrdersByScoreAscending_WhenMultipleDriversMatch()
  {
    // Arrange: both within the 5000m threshold, closeDriver (~1112m) nearer than farDriver (~3336m).
    await using var db = NewContext();
    var closeDriver = DriverAt(new Point(0, 0.01));
    var farDriver = DriverAt(new Point(0, 0.03));
    db.Drivers.AddRange(farDriver, closeDriver);
    await db.SaveChangesAsync();
    _mediator.Send(Arg.Any<GetUsersRatings>(), Arg.Any<CancellationToken>())
      .Returns(callInfo =>
      {
        var request = callInfo.Arg<GetUsersRatings>();
        return request!.UserIds!.ToDictionary(id => id, _ => 5.0);
      });
    var handler = new SearchBestDriverHandler(db, _weights, _mediator);

    // Act
    var result = await handler.Handle(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(0, 0), DistanceThresholdInMeters = 5000 }, CancellationToken.None);

    // Assert
    Assert.Equal(2, result.Count);
    Assert.Equal(closeDriver.Id, result[0].DriverId);
    Assert.Equal(farDriver.Id, result[1].DriverId);
  }

  [Fact]
  public async Task Handle_CapsCandidates_WhenManyDriversAreWithinThreshold()
  {
    // Arrange: five drivers all inside the threshold, but MaxCandidates allows two. Without the
    // cap every available driver would be materialised and every id forwarded to GetUsersRatings.
    await using var db = NewContext();
    for (var i = 1; i <= 5; i++)
      db.Drivers.Add(DriverAt(new Point(0, i * 0.001)));
    await db.SaveChangesAsync();

    _mediator.Send(Arg.Any<GetUsersRatings>(), Arg.Any<CancellationToken>())
      .Returns(new Dictionary<Guid, double>());

    var weights = Options.Create(new MatchingWeights
    {
      DistanceWeight = 0.5,
      RatingWeight = 0.5,
      UserMinRating = 0.0,
      UserMaxRating = 5.0,
      MaxCandidates = 2
    });
    var handler = new SearchBestDriverHandler(db, weights, _mediator);

    // Act
    var result = await handler.Handle(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(0, 0), DistanceThresholdInMeters = 5000 }, CancellationToken.None);

    // Assert
    Assert.Equal(2, result.Count);
  }
}
