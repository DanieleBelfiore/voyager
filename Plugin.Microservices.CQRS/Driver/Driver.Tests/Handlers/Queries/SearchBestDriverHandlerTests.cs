using Driver.Core.CQRS.Queries;
using Driver.Handlers.CQRS.Queries;
using Identity.Core.CQRS.Queries;
using Hikyaku;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.Geometries;
using NSubstitute;
using Xunit;

namespace Driver.Tests.Handlers.Queries;

/// <summary>
/// EF Core's InMemory provider evaluates Point.Distance() as NTS's own planar/Cartesian
/// distance on raw coordinate values, not SQL Server's geodetic STDistance the handler actually
/// runs against in production — so distances here are in "coordinate units", not meters, and
/// these tests validate the handler's filtering/scoring/wiring logic only. Real STDistance
/// correctness (units, geodetic accuracy) is covered by SearchBestDriverIntegrationTests
/// against a real SQL Server via Testcontainers.
/// </summary>
public class SearchBestDriverHandlerTests
{
  private readonly IHikyaku _mediator;
  private readonly TestApplicationDbContext _context;
  private readonly IConfiguration _configuration;

  public SearchBestDriverHandlerTests()
  {
    var (context, _) = TestBase.CreateTestServices();
    _context = context;
    _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
      ["DistanceWeight"] = "0.5",
      ["RatingWeight"] = "0.5",
      ["UserMinRating"] = "0",
      ["UserMaxRating"] = "5"
    }).Build();

    var mediatorMock = Substitute.For<IHikyaku>();
    _mediator = mediatorMock;

    mediatorMock.Send(Arg.Any<SearchBestDriver>(), Arg.Any<CancellationToken>())
      .Returns(c => new SearchBestDriverHandler(_context, _mediator, _configuration)
        .Handle(c.Arg<SearchBestDriver>(), c.Arg<CancellationToken>()));
  }

  private static Driver.Handlers.Models.Driver DriverAt(Point location) => new()
  {
    Id = Guid.NewGuid(),
    LastLocation = location
  };

  [Fact]
  public async Task Handle_ReturnsEmpty_WhenNoDriverWithinThreshold()
  {
    // Arrange: (100, 100) is far outside a threshold of 5 coordinate units under either
    // distance semantics.
    _context.Drivers.Add(DriverAt(new Point(100, 100)));
    await _context.SaveChangesAsync();

    // Act
    var result = await _mediator.Send(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(0, 0), DistanceThresholdInMeters = 5 });

    // Assert
    Assert.Empty(result);
  }

  [Fact]
  public async Task Handle_ReturnsDriver_WhenWithinThreshold()
  {
    // Arrange
    var driver = DriverAt(new Point(0, 0.01));
    _context.Drivers.Add(driver);
    await _context.SaveChangesAsync();
    _mediator.Send(Arg.Any<GetUsersRatings>(), Arg.Any<CancellationToken>())
      .Returns(new Dictionary<Guid, double> { [driver.Id] = 4.0 });

    // Act
    var result = await _mediator.Send(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(0, 0), DistanceThresholdInMeters = 5000 });

    // Assert
    var response = Assert.Single(result);
    Assert.Equal(driver.Id, response.DriverId);
    Assert.True(response.Distance >= 0);
  }

  [Fact]
  public async Task Handle_OrdersByScoreAscending_WhenMultipleDriversMatch()
  {
    // Arrange: both within threshold, closeDriver nearer than farDriver.
    var closeDriver = DriverAt(new Point(0, 0.01));
    var farDriver = DriverAt(new Point(0, 0.03));
    _context.Drivers.AddRange(closeDriver, farDriver);
    await _context.SaveChangesAsync();
    _mediator.Send(Arg.Any<GetUsersRatings>(), Arg.Any<CancellationToken>())
      .Returns(callInfo =>
      {
        var request = callInfo.Arg<GetUsersRatings>();
        return new Dictionary<Guid, double> { [request.UserIds[0]] = 5.0 };
      });

    // Act
    var result = await _mediator.Send(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(0, 0), DistanceThresholdInMeters = 5000 });

    // Assert
    Assert.Equal(2, result.Count);
    Assert.Equal(closeDriver.Id, result[0].DriverId);
    Assert.Equal(farDriver.Id, result[1].DriverId);
  }

  [Fact]
  public async Task Handle_RanksUnratedDriverAtMidpoint_NotAtTheFloor()
  {
    // Arrange: same distance, so rating alone decides. The unrated driver is absent from the
    // ratings dictionary and must fall back to (UserMinRating + UserMaxRating) / 2 = 2.5, which
    // beats a genuine 1.0. Identity used to return every registered user with Ratings = 0.0,
    // so the fallback never fired and a brand-new driver ranked below the worst-rated one.
    var unratedDriver = DriverAt(new Point(0, 0.01));
    var poorlyRatedDriver = DriverAt(new Point(0, 0.01));
    _context.Drivers.AddRange(unratedDriver, poorlyRatedDriver);
    await _context.SaveChangesAsync();

    _mediator.Send(Arg.Any<GetUsersRatings>(), Arg.Any<CancellationToken>())
      .Returns(new Dictionary<Guid, double> { [poorlyRatedDriver.Id] = 1.0 });

    // Act
    var result = await _mediator.Send(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(0, 0), DistanceThresholdInMeters = 5000 });

    // Assert
    Assert.Equal(2, result.Count);
    Assert.Equal(unratedDriver.Id, result[0].DriverId);
    Assert.True(result[0].Score < result[1].Score);
  }

  [Fact]
  public async Task Handle_CapsCandidates_WhenManyDriversAreWithinThreshold()
  {
    // Arrange: five drivers all inside the threshold, but MaxCandidates allows two. Without the
    // cap every available driver would be materialised and every id forwarded to GetUsersRatings.
    var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
      ["DistanceWeight"] = "0.5",
      ["RatingWeight"] = "0.5",
      ["UserMinRating"] = "0",
      ["UserMaxRating"] = "5",
      ["MaxCandidates"] = "2"
    }).Build();

    for (var i = 1; i <= 5; i++)
      _context.Drivers.Add(DriverAt(new Point(0, i * 0.001)));
    await _context.SaveChangesAsync();

    var handler = new SearchBestDriverHandler(_context, _mediator, configuration);

    // Act
    var result = await handler.Handle(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(0, 0), DistanceThresholdInMeters = 5000 }, CancellationToken.None);

    // Assert
    Assert.Equal(2, result.Count);
  }
}
