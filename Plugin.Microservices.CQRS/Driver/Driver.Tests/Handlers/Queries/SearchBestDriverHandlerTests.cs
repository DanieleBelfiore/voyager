using Driver.Core.CQRS.Queries;
using Driver.Handlers.CQRS.Queries;
using Identity.Core.CQRS.Queries;
using MediatR;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.Geometries;
using NSubstitute;
using Xunit;

namespace Driver.Tests.Handlers.Queries;

public class SearchBestDriverHandlerTests
{
  private readonly IMediator _mediator;
  private readonly TestApplicationDbContext _context;
  private readonly Common.Core.Cache.ICacheService _cache;
  private readonly IConfiguration _configuration;

  public SearchBestDriverHandlerTests()
  {
    var (context, cache) = TestBase.CreateTestServices();
    _context = context;
    _cache = cache;
    _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
      ["DistanceWeight"] = "0.5",
      ["RatingWeight"] = "0.5",
      ["UserMinRating"] = "0",
      ["UserMaxRating"] = "5"
    }).Build();

    var mediatorMock = Substitute.For<IMediator>();
    _mediator = mediatorMock;

    mediatorMock.Send(Arg.Any<SearchBestDriver>(), Arg.Any<CancellationToken>())
      .Returns(c => new SearchBestDriverHandler(_context, _mediator, _configuration, _cache)
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
    // Arrange
    _context.Drivers.Add(DriverAt(new Point(100, 100)));
    await _context.SaveChangesAsync();

    // Act
    var result = await _mediator.Send(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(0, 0), DistanceThresholdInMeters = 10 });

    // Assert
    Assert.Empty(result);
  }

  [Fact]
  public async Task Handle_ReturnsDriver_WhenWithinThreshold()
  {
    // Arrange
    var driver = DriverAt(new Point(0, 5));
    _context.Drivers.Add(driver);
    await _context.SaveChangesAsync();
    _mediator.Send(Arg.Any<GetUsersRatings>(), Arg.Any<CancellationToken>())
      .Returns(new Dictionary<Guid, double> { [driver.Id] = 4.0 });

    // Act
    var result = await _mediator.Send(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(0, 0), DistanceThresholdInMeters = 10 });

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
    _context.Drivers.AddRange(closeDriver, farDriver);
    await _context.SaveChangesAsync();
    _mediator.Send(Arg.Any<GetUsersRatings>(), Arg.Any<CancellationToken>())
      .Returns(callInfo =>
      {
        var request = callInfo.Arg<GetUsersRatings>();
        return new Dictionary<Guid, double> { [request.UserIds[0]] = 5.0 };
      });

    // Act
    var result = await _mediator.Send(new SearchBestDriver { UserId = Guid.NewGuid(), Location = new Point(0, 0), DistanceThresholdInMeters = 10 });

    // Assert
    Assert.Equal(2, result.Count);
    Assert.Equal(closeDriver.Id, result[0].DriverId);
    Assert.Equal(farDriver.Id, result[1].DriverId);
  }
}
