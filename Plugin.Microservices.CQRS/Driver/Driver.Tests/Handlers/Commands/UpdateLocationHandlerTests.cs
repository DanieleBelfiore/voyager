using Driver.Core.CQRS.Commands;
using Driver.Handlers.CQRS.Commands;
using MediatR;
using NetTopologySuite.Geometries;
using NSubstitute;
using Xunit;

namespace Driver.Tests.Handlers.Commands;

public class UpdateLocationHandlerTests
{
  private readonly IMediator _mediator;
  private readonly TestApplicationDbContext _context;
  private readonly Common.Core.Cache.ICacheService _cache;

  public UpdateLocationHandlerTests()
  {
    var (context, cache) = TestBase.CreateTestServices();
    _context = context;
    _cache = cache;

    var mediatorMock = Substitute.For<IMediator>();
    _mediator = mediatorMock;

    mediatorMock.Send(Arg.Any<UpdateLocation>(), Arg.Any<CancellationToken>())
      .Returns(c => new UpdateLocationHandler(_context, _cache)
        .Handle(c.Arg<UpdateLocation>(), c.Arg<CancellationToken>()));
  }

  [Fact]
  public async Task Handle_UpdatesLocationAndInvalidatesCache_WhenDriverExists()
  {
    // Arrange
    var id = Guid.NewGuid();
    _context.Drivers.Add(new Driver.Handlers.Models.Driver { Id = id });
    await _context.SaveChangesAsync();
    await _cache.GetOrCreateAsync($"driver:status:{id}", () => Task.FromResult("cached"), TimeSpan.FromMinutes(1));
    var location = new Point(1, 2);

    // Act
    await _mediator.Send(new UpdateLocation { Id = id, Location = location });

    // Assert
    var driver = await _context.Drivers.FindAsync(id);
    Assert.Equal(location, driver!.LastLocation);
    Assert.NotNull(driver.LastLocationGeoJSON);
    Assert.Null(await _cache.GetAsync<string>($"driver:status:{id}"));
  }

  [Fact]
  public async Task Handle_Throws_WhenDriverNotFound()
  {
    // Arrange
    var act = () => _mediator.Send(new UpdateLocation { Id = Guid.NewGuid(), Location = new Point(0, 0) });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("driver_not_found", ex.Message);
  }
}
