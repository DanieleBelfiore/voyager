using Driver.Core.Ports.Primary;
using Driver.Core.Ports.Secondary;
using Driver.Core.UseCases;
using DriverEntity = Driver.Core.Domain.Driver;
using NetTopologySuite.Geometries;
using NSubstitute;
using Xunit;

namespace Driver.Tests.UseCases;

public class SearchBestDriverUseCaseTests
{
  private readonly IDriverRepository _repository;
  private readonly IRatingsQueryService _ratingsQuery;
  private readonly IMatchingWeights _weights;
  private readonly SearchBestDriverUseCase _useCase;

  public SearchBestDriverUseCaseTests()
  {
    _repository = Substitute.For<IDriverRepository>();
    _ratingsQuery = Substitute.For<IRatingsQueryService>();
    _weights = Substitute.For<IMatchingWeights>();
    _weights.DistanceWeight.Returns(0.5);
    _weights.RatingWeight.Returns(0.5);
    _weights.UserMinRating.Returns(0.0);
    _weights.UserMaxRating.Returns(5.0);

    _useCase = new SearchBestDriverUseCase(_repository, _ratingsQuery, _weights);
  }

  private static DriverEntity DriverAt(Point location)
  {
    var driver = new DriverEntity(Guid.NewGuid());
    driver.UpdateLocation(location);
    return driver;
  }

  // The repository owns the distance filter/computation (real STDistance in production) — the
  // use case only sees whatever NearbyDriver rows it returns, so tests control distance
  // directly instead of relying on NTS's in-memory (non-geodetic) Point.Distance().
  private void ReturnsNearbyDrivers(params (DriverEntity Driver, double DistanceInMeters)[] drivers)
  {
    _repository.GetAvailableWithinDistanceAsync(Arg.Any<Point>(), Arg.Any<double>(), Arg.Any<CancellationToken>())
      .Returns(drivers.Select(d => new NearbyDriver { Driver = d.Driver, DistanceInMeters = d.DistanceInMeters }).ToList());
  }

  [Fact]
  public async Task Handle_ReturnsEmpty_WhenRepositoryFindsNoDriverWithinThreshold()
  {
    // Arrange: the repository itself is responsible for the distance cutoff (STDistance in the
    // WHERE clause), so "nothing within threshold" means it returns an empty set.
    ReturnsNearbyDrivers();

    // Act
    var result = await _useCase.Handle(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(0, 0), DistanceThresholdInMeters = 5000 }, CancellationToken.None);

    // Assert
    Assert.Empty(result);
    await _ratingsQuery.DidNotReceive().GetRatingsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_ReturnsDriver_WhenWithinThreshold()
  {
    // Arrange
    var driver = DriverAt(new Point(0, 0.01));
    ReturnsNearbyDrivers((driver, 1112.0));
    _ratingsQuery.GetRatingsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
      .Returns(new Dictionary<Guid, double> { [driver.Id] = 4.0 });

    // Act
    var result = await _useCase.Handle(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(0, 0), DistanceThresholdInMeters = 5000 }, CancellationToken.None);

    // Assert
    var response = Assert.Single(result);
    Assert.Equal(driver.Id, response.DriverId);
    Assert.Equal(1112.0, response.Distance);
  }

  [Fact]
  public async Task Handle_OrdersByScoreAscending_WhenMultipleDriversMatch()
  {
    // Arrange: both within threshold, closeDriver (1112m) nearer than farDriver (3336m), same rating.
    var closeDriver = DriverAt(new Point(0, 0.01));
    var farDriver = DriverAt(new Point(0, 0.03));
    ReturnsNearbyDrivers((farDriver, 3336.0), (closeDriver, 1112.0));
    _ratingsQuery.GetRatingsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
      .Returns(new Dictionary<Guid, double> { [closeDriver.Id] = 5.0, [farDriver.Id] = 5.0 });

    // Act
    var result = await _useCase.Handle(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(0, 0), DistanceThresholdInMeters = 5000 }, CancellationToken.None);

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
    ReturnsNearbyDrivers((driver, 1112.0));
    _ratingsQuery.GetRatingsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
      .Returns(new Dictionary<Guid, double>());

    // Act
    var result = await _useCase.Handle(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(0, 0), DistanceThresholdInMeters = 5000 }, CancellationToken.None);

    // Assert: normalizedRating = (2.5-0)/(5-0) = 0.5, normalizedDistance = 1112/5000 = 0.2224,
    // score = 0.5*0.2224 + 0.5*(1-0.5) = 0.3612. Under the old "default to 0.0" bug this would
    // have been 0.5*0.2224 + 0.5*(1-0) = 0.6112.
    var response = Assert.Single(result);
    Assert.InRange(response.Score, 0.35, 0.37);
  }
}
