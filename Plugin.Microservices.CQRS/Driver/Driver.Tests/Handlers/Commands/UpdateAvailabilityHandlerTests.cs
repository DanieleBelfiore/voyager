using Common.Core.Exceptions;
using Driver.Core.CQRS.Commands;
using Driver.Core.Enums;
using Driver.Handlers.CQRS.Commands;
using MediatR;
using NSubstitute;
using Xunit;

namespace Driver.Tests.Handlers.Commands;

public class UpdateAvailabilityHandlerTests
{
  private readonly IMediator _mediator;
  private readonly TestApplicationDbContext _context;
  private readonly Common.Core.Cache.ICacheService _cache;

  public UpdateAvailabilityHandlerTests()
  {
    var (context, cache) = TestBase.CreateTestServices();
    _context = context;
    _cache = cache;

    var mediatorMock = Substitute.For<IMediator>();
    _mediator = mediatorMock;

    mediatorMock.Send(Arg.Any<UpdateAvailability>(), Arg.Any<CancellationToken>())
      .Returns(c => new UpdateAvailabilityHandler(_context, _cache)
        .Handle(c.Arg<UpdateAvailability>(), c.Arg<CancellationToken>()));
  }

  [Fact]
  public async Task Handle_UpdatesStatusAndInvalidatesCache_WhenDriverExists()
  {
    // Arrange
    var id = Guid.NewGuid();
    _context.Drivers.Add(new Driver.Handlers.Models.Driver { Id = id });
    await _context.SaveChangesAsync();
    await _cache.GetOrCreateAsync($"driver:status:{id}", () => Task.FromResult("cached"), TimeSpan.FromMinutes(1));

    // Act
    await _mediator.Send(new UpdateAvailability { Id = id, Status = DriverStatus.OnRide });

    // Assert
    var driver = await _context.Drivers.FindAsync(id);
    Assert.Equal(DriverStatus.OnRide, driver!.Status);
    Assert.Null(await _cache.GetAsync<string>($"driver:status:{id}"));
  }

  [Fact]
  public async Task Handle_Throws_WhenDriverNotFound()
  {
    // Arrange
    var act = () => _mediator.Send(new UpdateAvailability { Id = Guid.NewGuid(), Status = DriverStatus.Available });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<NotFoundException>(act);
    Assert.Equal("driver_not_found", ex.Message);
  }
}
