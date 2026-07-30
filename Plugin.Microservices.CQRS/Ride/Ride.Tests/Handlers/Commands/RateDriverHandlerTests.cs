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

public class RateDriverHandlerTests
{
  private readonly IMediator _mediator;
  private readonly TestApplicationDbContext _context;
  private readonly IVoyagerShareClient _clientProxy;

  public RateDriverHandlerTests()
  {
    _context = TestBase.CreateTestDbContext();

    _clientProxy = Substitute.For<IVoyagerShareClient>();
    var clientsProxy = Substitute.For<IHubClients<IVoyagerShareClient>>();
    var hubContext = Substitute.For<IHubContext<VoyagerHub, IVoyagerShareClient>>();
    clientsProxy.Group(Arg.Any<string>()).Returns(_clientProxy);
    hubContext.Clients.Returns(clientsProxy);

    var mediatorMock = Substitute.For<IMediator>();
    _mediator = mediatorMock;

    mediatorMock.Send(Arg.Any<RateDriver>(), Arg.Any<CancellationToken>())
      .Returns(c => new RateDriverHandler(_context, _mediator, hubContext)
        .Handle(c.Arg<RateDriver>(), c.Arg<CancellationToken>()));
  }

  [Fact]
  public async Task Handle_UpdatesRatingAndPushesToHub_WhenDriverHasRides()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    var rideId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, UserId = userId, DriverId = driverId });
    await _context.SaveChangesAsync();
    var latestRide = new RideDetailsResponse { Id = rideId, DriverId = driverId };
    _mediator.Send(Arg.Any<GetRideDriverHistory>(), Arg.Any<CancellationToken>())
      .Returns([latestRide]);

    // Act
    await _mediator.Send(new RateDriver { RideId = rideId, CallerId = userId, Rating = 5 });

    // Assert
    await _mediator.Received(1).Send(Arg.Is<UpdateUserRating>(c => c.UserId == driverId && c.Rating == 5 && c.Rides == 1), Arg.Any<CancellationToken>());
    await _clientProxy.Received(1).SendToDriverNewRateReceived(5);
  }

  [Fact]
  public async Task Handle_DoesNotPushToHub_WhenDriverHasNoRides()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    var rideId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, UserId = userId, DriverId = driverId });
    await _context.SaveChangesAsync();
    _mediator.Send(Arg.Any<GetRideDriverHistory>(), Arg.Any<CancellationToken>())
      .Returns(new List<RideDetailsResponse>());

    // Act
    await _mediator.Send(new RateDriver { RideId = rideId, CallerId = userId, Rating = 5 });

    // Assert
    await _clientProxy.DidNotReceive().SendToDriverNewRateReceived(Arg.Any<int>());
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    var act = () => _mediator.Send(new RateDriver { RideId = Guid.NewGuid(), Rating = 3 });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("ride_not_found", ex.Message);
  }

  [Fact]
  public async Task Handle_Throws_WhenCallerIsNotRideOwner()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    var rideId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, UserId = userId, DriverId = driverId });
    await _context.SaveChangesAsync();
    var act = () => _mediator.Send(new RateDriver { RideId = rideId, CallerId = Guid.NewGuid(), Rating = 5 });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    Assert.Equal("not_ride_participant", ex.Message);
  }
}
