using AutoMapper;
using Common.Core.Cache;
using Driver.Core.CQRS.Queries;
using Driver.Core.Dtos;
using Driver.Core.Enums;
using Driver.Handlers;
using FluentAssertions;
using MediatR;
using NSubstitute;
using Xunit;

namespace Driver.Tests.Handlers.Queries
{
  public class GetDriverStatusHandlerTests
  {
    private readonly IMediator _mediator;
    private readonly TestApplicationDbContext _context;
    private readonly ICacheService _cache;

    public GetDriverStatusHandlerTests()
    {
      var (context, cache) = TestBase.CreateTestServices();
      _context = context;
      _cache = cache;

      var mediatorMock = Substitute.For<IMediator>();

      _mediator = mediatorMock;

      var config = new MapperConfiguration(cfg =>
      {
        cfg.AddProfile<MappingProfile>();
      });

      var mapper = config.CreateMapper();

      mediatorMock.Send(Arg.Any<GetDriverStatus>(), Arg.Any<CancellationToken>())
                  .Returns(c => new GetDriverStatusHandler(_context, mapper, _cache)
                  .Handle(c.Arg<GetDriverStatus>(), c.Arg<CancellationToken>()));
    }

    [Fact]
    public async Task GetDriverStatusTestFact()
    {
      // Arrange
      var id = Guid.NewGuid();

      _context.Drivers.Add(new Driver.Handlers.Models.Driver { Id = id });
      await _context.SaveChangesAsync();

      // Act
      var result = await _mediator.Send(new GetDriverStatus { Id = id });

      // Assert
      result.Should().NotBeNull();
      result.Id.Should().Be(id);
      result.Status.Should().Be(DriverStatus.Available);

      // Verify cache
      var cachedResult = await _cache.GetAsync<DriverStatusResponse>($"driver:status:{id}");
      cachedResult.Should().NotBeNull();
      cachedResult.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetDriverStatus_ShouldUseCacheOnSecondCall()
    {
      // Arrange
      var id = Guid.NewGuid();

      _context.Drivers.Add(new Driver.Handlers.Models.Driver { Id = id });
      await _context.SaveChangesAsync();

      // Act
      var firstResult = await _mediator.Send(new GetDriverStatus { Id = id });

      // Modify DB (shouldn't affect cached result)
      var driver = await _context.Drivers.FindAsync(id);
      driver!.Status = DriverStatus.OnRide;
      await _context.SaveChangesAsync();

      var secondResult = await _mediator.Send(new GetDriverStatus { Id = id });

      // Assert
      secondResult.Should().BeEquivalentTo(firstResult);
      secondResult.Status.Should().Be(DriverStatus.Available); // Still has cached value
    }
  }
}
