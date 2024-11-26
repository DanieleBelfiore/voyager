using FluentAssertions;
using MediatR;
using NetTopologySuite.Geometries;
using NSubstitute;
using Ride.Core.CQRS.Commands;
using Ride.Core.Enums;
using Ride.Handlers.CQRS.Commands;
using Xunit;

namespace Ride.Tests.Handlers.Commands
{
  public class StartRideHandlerTests
  {
    private readonly IMediator _mediator;
    private readonly TestApplicationDbContext _context;

    public StartRideHandlerTests()
    {
      _context = TestBase.CreateTestDbContext();

      var mediatorMock = Substitute.For<IMediator>();

      _mediator = mediatorMock;

      mediatorMock.Send(Arg.Any<StartRide>(), Arg.Any<CancellationToken>())
                  .Returns(c => new StartRideHandler(_context)
                  .Handle(c.Arg<StartRide>(), c.Arg<CancellationToken>()));
    }

    [Fact]
    public async Task StartRideTestFact()
    {
      // Arrange
      var id = Guid.NewGuid();

      _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = id });

      await _context.SaveChangesAsync(CancellationToken.None);

      var request = new StartRide { Id = id, Location = new Point(12.0, 42.0) };

      // Act
      await _mediator.Send(request, CancellationToken.None);

      // Assert
      var result = await _context.Rides.FindAsync(id);

      result.Should().NotBeNull();
      result!.Status.Should().Be(RideStatus.InProgress);
    }
  }
}
