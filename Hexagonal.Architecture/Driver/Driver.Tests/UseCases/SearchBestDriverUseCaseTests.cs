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
  private readonly ICacheService _cache;
  private readonly IMatchingWeights _weights;
  private readonly SearchBestDriverUseCase _useCase;

  public SearchBestDriverUseCaseTests()
  {
    _repository = Substitute.For<IDriverRepository>();
    _ratingsQuery = Substitute.For<IRatingsQueryService>();
    _cache = Substitute.For<ICacheService>();
    _weights = Substitute.For<IMatchingWeights>();
    _weights.DistanceWeight.Returns(0.5);
    _weights.RatingWeight.Returns(0.5);
    _weights.UserMinRating.Returns(0.0);
    _weights.UserMaxRating.Returns(5.0);

    _cache.GetOrCreateAsync(Arg.Any<string>(), Arg.Any<Func<Task<List<DriverEntity>>>>(), Arg.Any<TimeSpan>())
      .Returns(callInfo => callInfo.Arg<Func<Task<List<DriverEntity>>>>()());

    _useCase = new SearchBestDriverUseCase(_repository, _ratingsQuery, _cache, _weights);
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
    var farDriver = DriverAt(new Point(100, 100));
    _repository.GetAvailableWithLocationAsync(Arg.Any<CancellationToken>()).Returns([farDriver]);

    // Act
    var result = await _useCase.Handle(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(0, 0), DistanceThresholdInMeters = 10 }, CancellationToken.None);

    // Assert
    Assert.Empty(result);
    await _ratingsQuery.DidNotReceive().GetRatingsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_ReturnsDriver_WhenWithinThreshold()
  {
    // Arrange
    var driver = DriverAt(new Point(0, 5));
    _repository.GetAvailableWithLocationAsync(Arg.Any<CancellationToken>()).Returns([driver]);
    _ratingsQuery.GetRatingsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
      .Returns(new Dictionary<Guid, double> { [driver.Id] = 4.0 });

    // Act
    var result = await _useCase.Handle(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(0, 0), DistanceThresholdInMeters = 10 }, CancellationToken.None);

    // Assert
    var response = Assert.Single(result);
    Assert.Equal(driver.Id, response.DriverId);
    Assert.Equal(5, response.Distance);
  }

  [Fact]
  public async Task Handle_OrdersByScoreAscending_WhenMultipleDriversMatch()
  {
    // Arrange
    var closeDriver = DriverAt(new Point(0, 1));
    var farDriver = DriverAt(new Point(0, 9));
    _repository.GetAvailableWithLocationAsync(Arg.Any<CancellationToken>()).Returns([farDriver, closeDriver]);
    _ratingsQuery.GetRatingsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
      .Returns(new Dictionary<Guid, double> { [closeDriver.Id] = 5.0, [farDriver.Id] = 5.0 });

    // Act
    var result = await _useCase.Handle(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(0, 0), DistanceThresholdInMeters = 10 }, CancellationToken.None);

    // Assert
    Assert.Equal(2, result.Count);
    Assert.Equal(closeDriver.Id, result[0].DriverId);
    Assert.Equal(farDriver.Id, result[1].DriverId);
  }

  [Fact]
  public async Task Handle_DefaultsToZeroRating_WhenRatingsMissingForDriver()
  {
    // Arrange
    var driver = DriverAt(new Point(0, 1));
    _repository.GetAvailableWithLocationAsync(Arg.Any<CancellationToken>()).Returns([driver]);
    _ratingsQuery.GetRatingsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
      .Returns(new Dictionary<Guid, double>());

    // Act
    var result = await _useCase.Handle(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(0, 0), DistanceThresholdInMeters = 10 }, CancellationToken.None);

    // Assert
    Assert.Single(result);
  }
}
