using Hub.API;
using Hub.Core.Interfaces;
using Identity.Core.CQRS.Commands;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using Ride.Core.CQRS.Commands;
using Ride.Core.CQRS.Queries;
using Ride.Core.Dtos;
using Ride.Handlers.CQRS.Commands;
using Xunit;

namespace Ride.Tests.Handlers.Commands;

public class RateRideHandlerTests
{
  private readonly IMediator _mediator;
  private readonly TestApplicationDbContext _context;
  private readonly IVoyagerShareClient _clientProxy;

  public RateRideHandlerTests()
  {
    _context = TestBase.CreateTestDbContext();

    _clientProxy = Substitute.For<IVoyagerShareClient>();
    var clientsProxy = Substitute.For<IHubClients<IVoyagerShareClient>>();
    var hubContext = Substitute.For<IHubContext<VoyagerHub, IVoyagerShareClient>>();
    clientsProxy.Group(Arg.Any<string>()).Returns(_clientProxy);
    hubContext.Clients.Returns(clientsProxy);

    var mediatorMock = Substitute.For<IMediator>();
    _mediator = mediatorMock;

    mediatorMock.Send(Arg.Any<RateRide>(), Arg.Any<CancellationToken>())
      .Returns(c => new RateRideHandler(_context, _mediator, hubContext)
        .Handle(c.Arg<RateRide>(), c.Arg<CancellationToken>()));
  }

  [Fact]
  public async Task Handle_UpdatesRatingAndPushesToHub()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var rideId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, UserId = userId });
    await _context.SaveChangesAsync();
    _mediator.Send(Arg.Any<GetRideHistory>(), Arg.Any<CancellationToken>())
      .Returns([new RideDetailsResponse { Id = rideId, UserId = userId }]);

    // Act
    await _mediator.Send(new RateRide { RideId = rideId, Rating = 4 });

    // Assert
    await _mediator.Received(1).Send(Arg.Is<UpdateUserRating>(c => c.UserId == userId && c.Rating == 4 && c.Rides == 1), Arg.Any<CancellationToken>());
    await _clientProxy.Received(1).SendToRiderNewRateReceived(4);
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    var act = () => _mediator.Send(new RateRide { RideId = Guid.NewGuid(), Rating = 3 });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("ride_not_found", ex.Message);
  }
}
