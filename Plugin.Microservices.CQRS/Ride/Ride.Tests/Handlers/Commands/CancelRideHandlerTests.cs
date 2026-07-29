using Hub.API;
using Hub.Core.Interfaces;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using Ride.Core.CQRS.Commands;
using Ride.Core.Enums;
using Ride.Handlers.CQRS.Commands;
using Xunit;

namespace Ride.Tests.Handlers.Commands;

public class CancelRideHandlerTests
{
  private readonly IMediator _mediator;
  private readonly TestApplicationDbContext _context;
  private readonly IVoyagerShareClient _clientProxy;

  public CancelRideHandlerTests()
  {
    _context = TestBase.CreateTestDbContext();

    _clientProxy = Substitute.For<IVoyagerShareClient>();
    var clientsProxy = Substitute.For<IHubClients<IVoyagerShareClient>>();
    var hubContext = Substitute.For<IHubContext<VoyagerHub, IVoyagerShareClient>>();
    clientsProxy.Group(Arg.Any<string>()).Returns(_clientProxy);
    hubContext.Clients.Returns(clientsProxy);

    var mediatorMock = Substitute.For<IMediator>();
    _mediator = mediatorMock;

    mediatorMock.Send(Arg.Any<CancelRide>(), Arg.Any<CancellationToken>())
      .Returns(c => new CancelRideHandler(_context, hubContext)
        .Handle(c.Arg<CancelRide>(), c.Arg<CancellationToken>()));
  }

  [Fact]
  public async Task Handle_CancelsRideAndPushesToHub_WhenCancellable()
  {
    // Arrange
    var rideId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, Status = RideStatus.Requested });
    await _context.SaveChangesAsync();

    // Act
    await _mediator.Send(new CancelRide { Id = rideId, CancellationReason = "changed_mind" });

    // Assert
    var ride = await _context.Rides.FindAsync(rideId);
    Assert.Equal(RideStatus.Cancelled, ride!.Status);
    Assert.Equal("changed_mind", ride.CancellationReason);
    await _clientProxy.Received(1).SendToDriverRideCancel(rideId);
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    var act = () => _mediator.Send(new CancelRide { Id = Guid.NewGuid(), CancellationReason = "x" });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("no_ride_found", ex.Message);
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotCancellable()
  {
    // Arrange
    var rideId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, Status = RideStatus.InProgress });
    await _context.SaveChangesAsync();
    var act = () => _mediator.Send(new CancelRide { Id = rideId, CancellationReason = "x" });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("operation_not_permitted", ex.Message);
  }
}
