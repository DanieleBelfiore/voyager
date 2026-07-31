using Driver.Application.CQRS.Queries;
using Driver.Application.Ports;
using DriverEntity = Driver.Domain.Entities.Driver;
using NetTopologySuite.Geometries;
using NSubstitute;
using Xunit;

namespace Driver.Tests.Handlers.Queries;

public class SearchBestDriverHandlerTests
{
  private readonly IDriverRepository _repository;
  private readonly IRatingsQueryService _ratingsQuery;
  private readonly IMatchingWeights _weights;
  private readonly SearchBestDriverHandler _handler;

  public SearchBestDriverHandlerTests()
  {
    _repository = Substitute.For<IDriverRepository>();
    _ratingsQuery = Substitute.For<IRatingsQueryService>();
    _weights = Substitute.For<IMatchingWeights>();
    _weights.DistanceWeight.Returns(0.5);
    _weights.RatingWeight.Returns(0.5);
    _weights.UserMinRating.Returns(0.0);
    _weights.UserMaxRating.Returns(5.0);

    _handler = new SearchBestDriverHandler(_repository, _ratingsQuery, _weights);
  }

  private static DriverEntity DriverAt(Point location)
  {
    var driver = new DriverEntity(Guid.NewGuid());
    driver.UpdateLocation(location);
    return driver;
  }

  private void ReturnsFromBoundingBoxQuery(params DriverEntity[] drivers)
  {
    _repository.GetAvailableWithinBoundingBoxAsync(
      Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<CancellationToken>())
      .Returns(drivers.ToList());
  }

  [Fact]
  public async Task Handle_ReturnsEmpty_WhenNoDriverWithinThreshold()
  {
    // Arrange
    var farDriver = DriverAt(new Point(100, 100));
    ReturnsFromBoundingBoxQuery(farDriver);

    // Act
    var result = await _handler.Handle(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(0, 0), DistanceThresholdInMeters = 5000 }, CancellationToken.None);

    // Assert
    Assert.Empty(result);
    await _ratingsQuery.DidNotReceive().GetRatingsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_ReturnsDriver_WhenWithinThreshold()
  {
    // Arrange: 0.01 degrees of latitude is ~1112m — comfortably inside the 5000m threshold.
    var driver = DriverAt(new Point(0, 0.01));
    ReturnsFromBoundingBoxQuery(driver);
    _ratingsQuery.GetRatingsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
      .Returns(new Dictionary<Guid, double> { [driver.Id] = 4.0 });

    // Act
    var result = await _handler.Handle(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(0, 0), DistanceThresholdInMeters = 5000 }, CancellationToken.None);

    // Assert
    var response = Assert.Single(result);
    Assert.Equal(driver.Id, response.DriverId);
    Assert.InRange(response.Distance, 1100, 1120);
  }

  [Fact]
  public async Task Handle_OrdersByScoreAscending_WhenMultipleDriversMatch()
  {
    // Arrange: both within the 5000m threshold, closeDriver (~1112m) nearer than farDriver (~3336m).
    var closeDriver = DriverAt(new Point(0, 0.01));
    var farDriver = DriverAt(new Point(0, 0.03));
    ReturnsFromBoundingBoxQuery(farDriver, closeDriver);
    _ratingsQuery.GetRatingsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
      .Returns(new Dictionary<Guid, double> { [closeDriver.Id] = 5.0, [farDriver.Id] = 5.0 });

    // Act
    var result = await _handler.Handle(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(0, 0), DistanceThresholdInMeters = 5000 }, CancellationToken.None);

    // Assert
    Assert.Equal(2, result.Count);
    Assert.Equal(closeDriver.Id, result[0].DriverId);
    Assert.Equal(farDriver.Id, result[1].DriverId);
  }

  [Fact]
  public async Task Handle_DefaultsToMidpointRating_WhenRatingsMissingForDriver()
  {
    // Arrange: no ratings for this driver should score as "neutral" (midpoint of the 0-5
    // range = 2.5), not as the worst possible rating (0.0), which would unfairly bury
    // brand-new drivers in the ranking.
    var driver = DriverAt(new Point(0, 0.01));
    ReturnsFromBoundingBoxQuery(driver);
    _ratingsQuery.GetRatingsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
      .Returns(new Dictionary<Guid, double>());

    // Act
    var result = await _handler.Handle(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(0, 0), DistanceThresholdInMeters = 5000 }, CancellationToken.None);

    // Assert: normalizedRating = (2.5-0)/(5-0) = 0.5, normalizedDistance ~= 1112m/5000m = 0.222,
    // score = 0.5*0.222 + 0.5*(1-0.5) ~= 0.361. Under the old "default to 0.0" bug this would
    // have been 0.5*0.222 + 0.5*(1-0) ~= 0.611.
    var response = Assert.Single(result);
    Assert.InRange(response.Score, 0.35, 0.37);
  }
}
