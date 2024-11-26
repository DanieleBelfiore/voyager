using AutoMapper;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Ride.Core.CQRS.Queries;
using Ride.Core.Enums;
using Ride.Handlers;
using Ride.Handlers.CQRS.Queries;
using Xunit;

namespace Ride.Tests.Handlers.Queries
{
  public class GetActiveRideHandlerTests
  {
    private readonly IMediator _mediator;
    private readonly TestApplicationDbContext _context;

    public GetActiveRideHandlerTests()
    {
      _context = TestBase.CreateTestDbContext();

      var mediatorMock = Substitute.For<IMediator>();

      _mediator = mediatorMock;

      var config = new MapperConfiguration(cfg =>
      {
        cfg.AddProfile<MappingProfile>();
      });

      var mapper = config.CreateMapper();

      mediatorMock.Send(Arg.Any<GetActiveRide>(), Arg.Any<CancellationToken>())
                  .Returns(c => new GetActiveRideHandler(_context, mapper)
                  .Handle(c.Arg<GetActiveRide>(), c.Arg<CancellationToken>()));
    }

    [Fact]
    public async Task GetActiveRideTestFact()
    {
      // Arrange
      var userId = Guid.NewGuid();

      _context.Rides.Add(new Ride.Handlers.Models.Ride { UserId = userId, Status = RideStatus.Requested });

      await _context.SaveChangesAsync(CancellationToken.None);

      var request = new GetActiveRide { UserId = userId };

      // Act
      await _mediator.Send(request, CancellationToken.None);

      // Assert
      var result = await _context.Rides.Where(f => f.UserId == userId).FirstOrDefaultAsync();

      result.Should().NotBeNull();
      result!.Status.Should().Be(RideStatus.Requested);
    }
  }
}
