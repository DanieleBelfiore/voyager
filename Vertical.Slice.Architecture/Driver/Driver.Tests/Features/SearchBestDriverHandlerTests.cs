using Driver.Api.Entities;
using DriverEntity = Driver.Api.Entities.Driver;
using Driver.Api.Features.SearchBestDriver;
using Driver.Api.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using NSubstitute;
using System.Linq;
using Voyager.Contracts.Identity;
using Xunit;

namespace Driver.Tests.Features;

public class SearchBestDriverHandlerTests
{
  private readonly IMediator _mediator = Substitute.For<IMediator>();
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
    // Arrange
    await using var db = NewContext();
    var farDriver = DriverAt(new Point(100, 100));
    db.Drivers.Add(farDriver);
    await db.SaveChangesAsync();
    var handler = new SearchBestDriverHandler(db, _weights, _mediator);

    // Act
    var result = await handler.Handle(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(0, 0), DistanceThresholdInMeters = 5000 }, CancellationToken.None);

    // Assert
    Assert.Empty(result);
    await _mediator.DidNotReceive().Send(Arg.Any<GetUsersRatings>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_ReturnsDriver_WhenWithinThreshold()
  {
    // Arrange: 0.01 degrees of latitude is ~1112m — comfortably inside the 5000m threshold.
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
    Assert.InRange(response.Distance, 1100, 1120);
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
        return request.UserIds.ToDictionary(id => id, _ => 5.0);
      });
    var handler = new SearchBestDriverHandler(db, _weights, _mediator);

    // Act
    var result = await handler.Handle(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(0, 0), DistanceThresholdInMeters = 5000 }, CancellationToken.None);

    // Assert
    Assert.Equal(2, result.Count);
    Assert.Equal(closeDriver.Id, result[0].DriverId);
    Assert.Equal(farDriver.Id, result[1].DriverId);
  }
}
